using System.Text.RegularExpressions;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SalesService.Application.DTOs;
using SalesService.Application.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;
using SalesService.Domain.Events;
using SalesService.Domain.Exceptions;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.Commands;

// ══════════════════════════════════════════════════════════════════
// RecordFuelSale — Saga Step 1 (THE core sale command)
// ══════════════════════════════════════════════════════════════════
public record RecordFuelSaleCommand(
    Guid StationId, Guid PumpId, Guid TankId, Guid FuelTypeId,
    Guid DealerUserId, Guid? CustomerUserId, string VehicleNumber,
    decimal QuantityLitres, string PaymentMethod, string? PaymentReferenceId
) : IRequest<TransactionDto>;

public partial class RecordFuelSaleValidator : AbstractValidator<RecordFuelSaleCommand>
{
    [GeneratedRegex(@"^[A-Z]{2}[0-9]{2}[A-Z]{2}[0-9]{4}$")]
    private static partial Regex VehicleRegex();

    public RecordFuelSaleValidator()
    {
        RuleFor(x => x.StationId).NotEmpty();
        RuleFor(x => x.PumpId).NotEmpty();
        RuleFor(x => x.TankId).NotEmpty();
        RuleFor(x => x.FuelTypeId).NotEmpty();
        RuleFor(x => x.DealerUserId).NotEmpty();
        RuleFor(x => x.VehicleNumber).NotEmpty()
            .Must(v => VehicleRegex().IsMatch(v))
            .WithMessage("Vehicle number must match Indian RTO format: XX00XX0000");
        RuleFor(x => x.QuantityLitres).GreaterThan(0)
            .Must(q => decimal.Round(q, 3) == q)
            .WithMessage("Quantity must have at most 3 decimal places (Weights & Measures Act).");
        RuleFor(x => x.PaymentMethod).NotEmpty();
        RuleFor(x => x.PaymentReferenceId)
            .NotEmpty()
            .When(x => x.PaymentMethod != nameof(Domain.Enums.PaymentMethod.Cash))
            .WithMessage("PaymentReferenceId is required for non-cash payments.");
    }
}

public class RecordFuelSaleHandler(
    ITransactionRepository txRepo, IPumpRepository pumpRepo,
    IFuelPriceRepository priceRepo, IRabbitMqPublisher publisher,
    ILogger<RecordFuelSaleHandler> logger)
    : IRequestHandler<RecordFuelSaleCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(RecordFuelSaleCommand cmd, CancellationToken ct)
    {
        // Rule 7.3: Pump must be Active
        var pump = await pumpRepo.GetByIdAsync(cmd.PumpId, ct)
            ?? throw new NotFoundException("Pump", cmd.PumpId);
        if (pump.Status != PumpStatus.Active)
            throw new PumpNotActiveException(cmd.PumpId, pump.Status.ToString());

        // Rule 7.3: Snapshot current price
        var price = await priceRepo.GetActivePriceAsync(cmd.FuelTypeId, ct)
            ?? throw new DomainException($"No active price for fuel type {cmd.FuelTypeId}.");

        // Rule 7.3: Calculate total
        var totalAmount = Math.Round(cmd.QuantityLitres * price.PricePerLitre, 2, MidpointRounding.AwayFromZero);

        // Rule 7.3: Parse payment method
        if (!Enum.TryParse<PaymentMethod>(cmd.PaymentMethod, out var paymentMethod))
            throw new DomainException($"Invalid payment method: {cmd.PaymentMethod}");

        // Rule 7.3: Generate receipt number — yyyyMMdd-{StationCode}-{4-digit-seq}
        var today = DateTimeOffset.UtcNow;
        var seq = await txRepo.GetDailySequenceAsync(cmd.StationId, today, ct) + 1;
        // We use stationId short form as code (first 4 chars of GUID). In production this would query Station service.
        var stationCode = cmd.StationId.ToString()[..4].ToUpper();
        var receiptNumber = $"{today:yyyyMMdd}-{stationCode}-{seq:D4}";

        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            StationId = cmd.StationId,
            PumpId = cmd.PumpId,
            FuelTypeId = cmd.FuelTypeId,
            DealerUserId = cmd.DealerUserId,
            CustomerUserId = cmd.CustomerUserId,
            VehicleNumber = cmd.VehicleNumber,
            QuantityLitres = cmd.QuantityLitres,
            PricePerLitre = price.PricePerLitre,
            TotalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            PaymentReferenceId = cmd.PaymentReferenceId,
            Status = TransactionStatus.Initiated,
            FraudCheckStatus = FraudCheckStatus.Pending,
            Timestamp = today
        };

        await txRepo.AddAsync(tx, ct);

        // Saga Step 1: Publish SaleInitiatedEvent
        await publisher.PublishAsync(new SaleInitiatedEvent
        {
            EventType = nameof(SaleInitiatedEvent),
            TransactionId = tx.Id, StationId = cmd.StationId,
            TankId = cmd.TankId, FuelTypeId = cmd.FuelTypeId,
            QuantityLitres = cmd.QuantityLitres, DealerUserId = cmd.DealerUserId
        }, "sales.initiated", ct);

        logger.LogInformation("Sale initiated. Tx: {TxId}, Receipt: {Receipt}, Qty: {Qty}L, Total: ₹{Total}",
            tx.Id, receiptNumber, cmd.QuantityLitres, totalAmount);

        return MapToDto(tx);
    }

    private static TransactionDto MapToDto(Transaction t) => new(
        t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
        t.DealerUserId, t.CustomerUserId, t.VehicleNumber,
        t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
        t.PaymentMethod.ToString(), t.PaymentReferenceId, t.Status.ToString(),
        t.FraudCheckStatus.ToString(), t.LoyaltyPointsEarned, t.LoyaltyPointsRedeemed,
        t.Timestamp, t.IsVoided);
}

// ══════════════════════════════════════════════════════════════════
// VoidTransaction
// ══════════════════════════════════════════════════════════════════
public record VoidTransactionCommand(Guid TransactionId, Guid VoidedByUserId, string Reason) : IRequest<MessageResponseDto>;

public class VoidTransactionHandler(
    ITransactionRepository txRepo, IVoidedTransactionRepository voidRepo,
    IRabbitMqPublisher publisher, ILogger<VoidTransactionHandler> logger)
    : IRequestHandler<VoidTransactionCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(VoidTransactionCommand cmd, CancellationToken ct)
    {
        var tx = await txRepo.GetByIdAsync(cmd.TransactionId, ct)
            ?? throw new NotFoundException("Transaction", cmd.TransactionId);
        if (tx.IsVoided) throw new DomainException("Transaction is already voided.");
        if (tx.Status == TransactionStatus.Initiated) throw new DomainException("Cannot void a transaction still in Initiated status.");

        tx.Status = TransactionStatus.Voided;
        tx.IsVoided = true;
        await txRepo.UpdateAsync(tx, ct);

        await voidRepo.AddAsync(new VoidedTransaction
        {
            Id = Guid.NewGuid(), OriginalTransactionId = tx.Id,
            Reason = cmd.Reason, VoidedByUserId = cmd.VoidedByUserId
        }, ct);

        await publisher.PublishAsync(new TransactionVoidedEvent
        {
            EventType = nameof(TransactionVoidedEvent),
            TransactionId = tx.Id, StationId = tx.StationId,
            FuelTypeId = tx.FuelTypeId, QuantityLitres = tx.QuantityLitres,
            VoidedByUserId = cmd.VoidedByUserId, Reason = cmd.Reason
        }, "sales.voided", ct);

        logger.LogInformation("Transaction voided. Tx: {TxId}, Reason: {Reason}", tx.Id, cmd.Reason);
        return new MessageResponseDto("Transaction voided successfully.");
    }
}

// ══════════════════════════════════════════════════════════════════
// Pump Commands
// ══════════════════════════════════════════════════════════════════
public record RegisterPumpCommand(Guid StationId, Guid FuelTypeId, string PumpName, int NozzleCount) : IRequest<PumpDto>;

public class RegisterPumpHandler(IPumpRepository pumpRepo) : IRequestHandler<RegisterPumpCommand, PumpDto>
{
    public async Task<PumpDto> Handle(RegisterPumpCommand cmd, CancellationToken ct)
    {
        var pump = new Pump
        {
            Id = Guid.NewGuid(), StationId = cmd.StationId, FuelTypeId = cmd.FuelTypeId,
            PumpName = cmd.PumpName, NozzleCount = cmd.NozzleCount, Status = PumpStatus.Active
        };
        await pumpRepo.AddAsync(pump, ct);
        return new PumpDto(pump.Id, pump.StationId, pump.FuelTypeId, pump.PumpName,
            pump.NozzleCount, pump.Status.ToString(), pump.LastServiced, pump.NextServiceDue, pump.CreatedAt);
    }
}

public record UpdatePumpStatusCommand(Guid PumpId, string Status, string? Notes) : IRequest<PumpDto>;

public class UpdatePumpStatusHandler(IPumpRepository pumpRepo) : IRequestHandler<UpdatePumpStatusCommand, PumpDto>
{
    public async Task<PumpDto> Handle(UpdatePumpStatusCommand cmd, CancellationToken ct)
    {
        var pump = await pumpRepo.GetByIdAsync(cmd.PumpId, ct)
            ?? throw new NotFoundException("Pump", cmd.PumpId);
        if (!Enum.TryParse<PumpStatus>(cmd.Status, out var status))
            throw new DomainException($"Invalid pump status: {cmd.Status}");
        pump.Status = status; pump.UpdatedAt = DateTimeOffset.UtcNow;
        await pumpRepo.UpdateAsync(pump, ct);
        return new PumpDto(pump.Id, pump.StationId, pump.FuelTypeId, pump.PumpName,
            pump.NozzleCount, pump.Status.ToString(), pump.LastServiced, pump.NextServiceDue, pump.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// FuelPrice Command
// ══════════════════════════════════════════════════════════════════
public record SetFuelPriceCommand(Guid FuelTypeId, decimal PricePerLitre, DateTimeOffset EffectiveFrom, Guid SetByUserId) : IRequest<FuelPriceDto>;

public class SetFuelPriceHandler(
    IFuelPriceRepository priceRepo, IRabbitMqPublisher publisher,
    ILogger<SetFuelPriceHandler> logger) : IRequestHandler<SetFuelPriceCommand, FuelPriceDto>
{
    public async Task<FuelPriceDto> Handle(SetFuelPriceCommand cmd, CancellationToken ct)
    {
        await priceRepo.DeactivateAsync(cmd.FuelTypeId, ct);
        var price = new FuelPrice
        {
            Id = Guid.NewGuid(), FuelTypeId = cmd.FuelTypeId,
            PricePerLitre = cmd.PricePerLitre, EffectiveFrom = cmd.EffectiveFrom,
            IsActive = true, SetByUserId = cmd.SetByUserId
        };
        await priceRepo.AddAsync(price, ct);

        await publisher.PublishAsync(new FuelPriceUpdatedEvent
        {
            EventType = nameof(FuelPriceUpdatedEvent),
            FuelTypeId = cmd.FuelTypeId, NewPricePerLitre = cmd.PricePerLitre,
            EffectiveFrom = cmd.EffectiveFrom, UpdatedByUserId = cmd.SetByUserId
        }, "sales.price.updated", ct);

        logger.LogInformation("Fuel price set. FuelType: {FuelTypeId}, Price: ₹{Price}/L", cmd.FuelTypeId, cmd.PricePerLitre);
        return new FuelPriceDto(price.Id, price.FuelTypeId, price.PricePerLitre,
            price.EffectiveFrom, price.IsActive, price.SetByUserId, price.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// Shift Commands
// ══════════════════════════════════════════════════════════════════
public record StartShiftCommand(Guid DealerUserId, Guid StationId, string? Notes) : IRequest<ShiftDto>;

public class StartShiftHandler(IShiftRepository shiftRepo, ILogger<StartShiftHandler> logger) : IRequestHandler<StartShiftCommand, ShiftDto>
{
    public async Task<ShiftDto> Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var active = await shiftRepo.GetActiveShiftAsync(cmd.DealerUserId, ct);
        if (active != null) throw new DomainException("An active shift already exists. End the current shift before starting a new one.");

        var shift = new Shift
        {
            Id = Guid.NewGuid(), DealerUserId = cmd.DealerUserId, StationId = cmd.StationId,
            StartedAt = DateTimeOffset.UtcNow, OpeningStockJson = "{}"
        };
        await shiftRepo.AddAsync(shift, ct);
        logger.LogInformation("Shift started. Dealer: {DealerId}, Station: {StationId}", cmd.DealerUserId, cmd.StationId);
        return MapShift(shift);
    }

    private static ShiftDto MapShift(Shift s) => new(s.Id, s.DealerUserId, s.StationId, s.StartedAt, s.EndedAt,
        s.OpeningStockJson, s.ClosingStockJson, s.TotalLitresSold, s.TotalRevenue, s.TotalTransactions, s.DiscrepancyFlagged);
}

public record EndShiftCommand(Guid DealerUserId, string? Notes) : IRequest<ShiftDto>;

public class EndShiftHandler(IShiftRepository shiftRepo, ILogger<EndShiftHandler> logger) : IRequestHandler<EndShiftCommand, ShiftDto>
{
    public async Task<ShiftDto> Handle(EndShiftCommand cmd, CancellationToken ct)
    {
        var shift = await shiftRepo.GetActiveShiftAsync(cmd.DealerUserId, ct)
            ?? throw new DomainException("No active shift found for this dealer.");
        shift.EndedAt = DateTimeOffset.UtcNow;
        shift.ClosingStockJson = "{}";
        await shiftRepo.UpdateAsync(shift, ct);
        logger.LogInformation("Shift ended. Id: {ShiftId}, Dealer: {DealerId}", shift.Id, cmd.DealerUserId);
        return new ShiftDto(shift.Id, shift.DealerUserId, shift.StationId, shift.StartedAt, shift.EndedAt,
            shift.OpeningStockJson, shift.ClosingStockJson, shift.TotalLitresSold, shift.TotalRevenue, shift.TotalTransactions, shift.DiscrepancyFlagged);
    }
}

// ══════════════════════════════════════════════════════════════════
// Vehicle & Fleet Commands
// ══════════════════════════════════════════════════════════════════
public record RegisterVehicleCommand(Guid CustomerId, string RegistrationNumber, Guid? FuelTypePreference, string VehicleType, string? Nickname) : IRequest<VehicleDto>;

public class RegisterVehicleHandler(IRegisteredVehicleRepository vRepo) : IRequestHandler<RegisterVehicleCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(RegisterVehicleCommand cmd, CancellationToken ct)
    {
        var existing = await vRepo.GetByRegistrationAsync(cmd.RegistrationNumber, ct);
        if (existing != null) throw new DuplicateEntityException("Vehicle", "registration", cmd.RegistrationNumber);
        Enum.TryParse<VehicleType>(cmd.VehicleType, out var vt);
        var v = new RegisteredVehicle { Id = Guid.NewGuid(), CustomerId = cmd.CustomerId, RegistrationNumber = cmd.RegistrationNumber,
            FuelTypePreference = cmd.FuelTypePreference, VehicleType = vt, Nickname = cmd.Nickname };
        await vRepo.AddAsync(v, ct);
        return new VehicleDto(v.Id, v.CustomerId, v.RegistrationNumber, v.FuelTypePreference, v.VehicleType.ToString(), v.Nickname, v.IsActive, v.RegisteredAt);
    }
}

public record CreateFleetAccountCommand(string CompanyName, Guid ContactUserId, decimal CreditLimit) : IRequest<FleetAccountDto>;

public class CreateFleetAccountHandler(IFleetAccountRepository faRepo) : IRequestHandler<CreateFleetAccountCommand, FleetAccountDto>
{
    public async Task<FleetAccountDto> Handle(CreateFleetAccountCommand cmd, CancellationToken ct)
    {
        var fa = new FleetAccount { Id = Guid.NewGuid(), CompanyName = cmd.CompanyName, ContactUserId = cmd.ContactUserId, CreditLimit = cmd.CreditLimit };
        await faRepo.AddAsync(fa, ct);
        return new FleetAccountDto(fa.Id, fa.CompanyName, fa.ContactUserId, fa.CreditLimit, fa.CurrentBalance, fa.IsActive, fa.CreatedAt);
    }
}

public record AddVehicleToFleetCommand(Guid AccountId, Guid VehicleId, decimal? DailyLimitLitres, decimal? MonthlyLimitAmount) : IRequest<FleetVehicleDto>;

public class AddVehicleToFleetHandler(IFleetVehicleRepository fvRepo, IFleetAccountRepository faRepo) : IRequestHandler<AddVehicleToFleetCommand, FleetVehicleDto>
{
    public async Task<FleetVehicleDto> Handle(AddVehicleToFleetCommand cmd, CancellationToken ct)
    {
        _ = await faRepo.GetByIdAsync(cmd.AccountId, ct) ?? throw new NotFoundException("FleetAccount", cmd.AccountId);
        var fv = new FleetVehicle { Id = Guid.NewGuid(), FleetAccountId = cmd.AccountId, VehicleId = cmd.VehicleId,
            DailyLimitLitres = cmd.DailyLimitLitres, MonthlyLimitAmount = cmd.MonthlyLimitAmount };
        await fvRepo.AddAsync(fv, ct);
        return new FleetVehicleDto(fv.Id, fv.FleetAccountId, fv.VehicleId, fv.DailyLimitLitres, fv.MonthlyLimitAmount, fv.IsActive);
    }
}

public record RemoveFleetVehicleCommand(Guid AccountId, Guid VehicleId) : IRequest<MessageResponseDto>;

public class RemoveFleetVehicleHandler(IFleetVehicleRepository fvRepo) : IRequestHandler<RemoveFleetVehicleCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(RemoveFleetVehicleCommand cmd, CancellationToken ct)
    {
        var fv = await fvRepo.GetByIdAsync(cmd.VehicleId, ct) ?? throw new NotFoundException("FleetVehicle", cmd.VehicleId);
        await fvRepo.RemoveAsync(fv, ct);
        return new MessageResponseDto("Fleet vehicle removed.");
    }
}

// ══════════════════════════════════════════════════════════════════
// Wallet Commands (Razorpay)
// ══════════════════════════════════════════════════════════════════
public record CreateWalletOrderCommand(Guid CustomerId, decimal Amount) : IRequest<CreateOrderResponseDto>;

public class CreateWalletOrderHandler(
    ICustomerWalletRepository walletRepo, IWalletTransactionRepository wtRepo,
    IRazorpayService razorpay, ILogger<CreateWalletOrderHandler> logger) : IRequestHandler<CreateWalletOrderCommand, CreateOrderResponseDto>
{
    public async Task<CreateOrderResponseDto> Handle(CreateWalletOrderCommand cmd, CancellationToken ct)
    {
        // Ensure wallet exists
        var wallet = await walletRepo.GetByCustomerIdAsync(cmd.CustomerId, ct);
        if (wallet == null)
        {
            wallet = new CustomerWallet { Id = Guid.NewGuid(), CustomerId = cmd.CustomerId };
            await walletRepo.AddAsync(wallet, ct);
        }

        var order = await razorpay.CreateOrderAsync(cmd.Amount);

        await wtRepo.AddAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(), WalletId = wallet.Id,
            Type = WalletTransactionType.TopUp, Amount = cmd.Amount, BalanceAfter = wallet.Balance,
            RazorpayOrderId = order.OrderId, Status = WalletTransactionStatus.Pending,
            Description = $"Wallet top-up ₹{cmd.Amount:F2}"
        }, ct);

        logger.LogInformation("Razorpay order created. Customer: {CustId}, Amount: ₹{Amt}, Order: {OrderId}",
            cmd.CustomerId, cmd.Amount, order.OrderId);

        return new CreateOrderResponseDto(order.OrderId, order.Amount, order.Currency, order.KeyId);
    }
}

public record VerifyWalletPaymentCommand(Guid CustomerId, string OrderId, string PaymentId, string Signature) : IRequest<MessageResponseDto>;

public class VerifyWalletPaymentHandler(
    ICustomerWalletRepository walletRepo, IWalletTransactionRepository wtRepo,
    IRazorpayService razorpay, ILogger<VerifyWalletPaymentHandler> logger) : IRequestHandler<VerifyWalletPaymentCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(VerifyWalletPaymentCommand cmd, CancellationToken ct)
    {
        var wt = await wtRepo.GetByRazorpayOrderIdAsync(cmd.OrderId, ct)
            ?? throw new NotFoundException("WalletTransaction (order)", cmd.OrderId);

        if (!razorpay.VerifyPaymentSignature(cmd.OrderId, cmd.PaymentId, cmd.Signature))
        {
            wt.Status = WalletTransactionStatus.Failed;
            await wtRepo.UpdateAsync(wt, ct);
            throw new DomainException("Payment signature verification failed.");
        }

        var wallet = await walletRepo.GetByCustomerIdAsync(cmd.CustomerId, ct)
            ?? throw new NotFoundException("Wallet", cmd.CustomerId);

        wallet.Balance += wt.Amount;
        wallet.TotalLoaded += wt.Amount;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        await walletRepo.UpdateAsync(wallet, ct);

        wt.Status = WalletTransactionStatus.Captured;
        wt.RazorpayPaymentId = cmd.PaymentId;
        wt.BalanceAfter = wallet.Balance;
        await wtRepo.UpdateAsync(wt, ct);

        logger.LogInformation("Wallet top-up verified. Customer: {CustId}, Amount: ₹{Amt}, Balance: ₹{Bal}",
            cmd.CustomerId, wt.Amount, wallet.Balance);
        return new MessageResponseDto($"Wallet topped up by ₹{wt.Amount:F2}. New balance: ₹{wallet.Balance:F2}.");
    }
}
