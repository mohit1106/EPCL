using MediatR;
using SalesService.Application.Common;
using SalesService.Application.DTOs;
using SalesService.Domain.Enums;
using SalesService.Domain.Exceptions;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.Queries;

// ── Transactions ───────────────────────────────────────────────────
public record GetTransactionsQuery(int Page = 1, int PageSize = 20, Guid? StationId = null,
    Guid? DealerId = null, Guid? CustomerId = null, string? VehicleNumber = null,
    string? Status = null) : IRequest<PagedResult<TransactionDto>>;

public class GetTransactionsHandler(ITransactionRepository txRepo) : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery q, CancellationToken ct)
    {
        TransactionStatus? statusFilter = null;
        if (q.Status != null && Enum.TryParse<TransactionStatus>(q.Status, out var s)) statusFilter = s;

        var (items, total) = await txRepo.GetPagedAsync(q.Page, q.PageSize, q.StationId, q.DealerId,
            q.CustomerId, q.VehicleNumber, statusFilter, ct);

        return new PagedResult<TransactionDto>
        {
            Items = items.Select(t => MapTx(t)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }

    private static TransactionDto MapTx(Domain.Entities.Transaction t) => new(
        t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
        t.DealerUserId, t.CustomerUserId, t.VehicleNumber,
        t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
        t.PaymentMethod.ToString(), t.PaymentReferenceId, t.Status.ToString(),
        t.FraudCheckStatus.ToString(), t.LoyaltyPointsEarned, t.LoyaltyPointsRedeemed,
        t.Timestamp, t.IsVoided);
}

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;

public class GetTransactionByIdHandler(ITransactionRepository txRepo) : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionByIdQuery q, CancellationToken ct)
    {
        var t = await txRepo.GetByIdAsync(q.TransactionId, ct) ?? throw new NotFoundException("Transaction", q.TransactionId);
        return new TransactionDto(t.Id, t.ReceiptNumber, t.StationId, t.PumpId, t.FuelTypeId,
            t.DealerUserId, t.CustomerUserId, t.VehicleNumber, t.QuantityLitres, t.PricePerLitre, t.TotalAmount,
            t.PaymentMethod.ToString(), t.PaymentReferenceId, t.Status.ToString(), t.FraudCheckStatus.ToString(),
            t.LoyaltyPointsEarned, t.LoyaltyPointsRedeemed, t.Timestamp, t.IsVoided);
    }
}

// ── Pumps ──────────────────────────────────────────────────────────
public record GetPumpsByStationQuery(Guid StationId) : IRequest<IReadOnlyList<PumpDto>>;

public class GetPumpsByStationHandler(IPumpRepository pumpRepo) : IRequestHandler<GetPumpsByStationQuery, IReadOnlyList<PumpDto>>
{
    public async Task<IReadOnlyList<PumpDto>> Handle(GetPumpsByStationQuery q, CancellationToken ct)
    {
        var pumps = await pumpRepo.GetByStationIdAsync(q.StationId, ct);
        return pumps.Select(p => new PumpDto(p.Id, p.StationId, p.FuelTypeId, p.PumpName,
            p.NozzleCount, p.Status.ToString(), p.LastServiced, p.NextServiceDue, p.CreatedAt)).ToList();
    }
}

// ── Fuel Prices ────────────────────────────────────────────────────
public record GetActiveFuelPricesQuery() : IRequest<IReadOnlyList<FuelPriceDto>>;

public class GetActiveFuelPricesHandler(IFuelPriceRepository priceRepo) : IRequestHandler<GetActiveFuelPricesQuery, IReadOnlyList<FuelPriceDto>>
{
    public async Task<IReadOnlyList<FuelPriceDto>> Handle(GetActiveFuelPricesQuery q, CancellationToken ct)
    {
        var prices = await priceRepo.GetAllActiveAsync(ct);
        return prices.Select(p => new FuelPriceDto(p.Id, p.FuelTypeId, p.PricePerLitre,
            p.EffectiveFrom, p.IsActive, p.SetByUserId, p.CreatedAt)).ToList();
    }
}

// ── Shifts ─────────────────────────────────────────────────────────
public record GetActiveShiftQuery(Guid DealerUserId) : IRequest<ShiftDto?>;

public class GetActiveShiftHandler(IShiftRepository shiftRepo) : IRequestHandler<GetActiveShiftQuery, ShiftDto?>
{
    public async Task<ShiftDto?> Handle(GetActiveShiftQuery q, CancellationToken ct)
    {
        var s = await shiftRepo.GetActiveShiftAsync(q.DealerUserId, ct);
        if (s == null) return null;
        return new ShiftDto(s.Id, s.DealerUserId, s.StationId, s.StartedAt, s.EndedAt,
            s.OpeningStockJson, s.ClosingStockJson, s.TotalLitresSold, s.TotalRevenue, s.TotalTransactions, s.DiscrepancyFlagged);
    }
}

// ── Vehicles ───────────────────────────────────────────────────────
public record GetCustomerVehiclesQuery(Guid CustomerId) : IRequest<IReadOnlyList<VehicleDto>>;

public class GetCustomerVehiclesHandler(IRegisteredVehicleRepository vRepo) : IRequestHandler<GetCustomerVehiclesQuery, IReadOnlyList<VehicleDto>>
{
    public async Task<IReadOnlyList<VehicleDto>> Handle(GetCustomerVehiclesQuery q, CancellationToken ct)
    {
        var vs = await vRepo.GetByCustomerAsync(q.CustomerId, ct);
        return vs.Select(v => new VehicleDto(v.Id, v.CustomerId, v.RegistrationNumber,
            v.FuelTypePreference, v.VehicleType.ToString(), v.Nickname, v.IsActive, v.RegisteredAt)).ToList();
    }
}

// ── Fleet ──────────────────────────────────────────────────────────
public record GetFleetAccountsQuery() : IRequest<IReadOnlyList<FleetAccountDto>>;

public class GetFleetAccountsHandler(IFleetAccountRepository faRepo) : IRequestHandler<GetFleetAccountsQuery, IReadOnlyList<FleetAccountDto>>
{
    public async Task<IReadOnlyList<FleetAccountDto>> Handle(GetFleetAccountsQuery q, CancellationToken ct)
    {
        var fas = await faRepo.GetAllAsync(ct);
        return fas.Select(f => new FleetAccountDto(f.Id, f.CompanyName, f.ContactUserId,
            f.CreditLimit, f.CurrentBalance, f.IsActive, f.CreatedAt)).ToList();
    }
}

// ── Wallet ─────────────────────────────────────────────────────────
public record GetWalletBalanceQuery(Guid CustomerId) : IRequest<WalletDto>;

public class GetWalletBalanceHandler(ICustomerWalletRepository walletRepo) : IRequestHandler<GetWalletBalanceQuery, WalletDto>
{
    public async Task<WalletDto> Handle(GetWalletBalanceQuery q, CancellationToken ct)
    {
        var w = await walletRepo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("Wallet", q.CustomerId);
        return new WalletDto(w.Id, w.CustomerId, w.Balance, w.TotalLoaded, w.IsActive);
    }
}

public record GetWalletHistoryQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<WalletTransactionDto>>;

public class GetWalletHistoryHandler(ICustomerWalletRepository walletRepo, IWalletTransactionRepository wtRepo)
    : IRequestHandler<GetWalletHistoryQuery, IReadOnlyList<WalletTransactionDto>>
{
    public async Task<IReadOnlyList<WalletTransactionDto>> Handle(GetWalletHistoryQuery q, CancellationToken ct)
    {
        var w = await walletRepo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("Wallet", q.CustomerId);
        var items = await wtRepo.GetByWalletIdAsync(w.Id, q.Page, q.PageSize, ct);
        return items.Select(t => new WalletTransactionDto(t.Id, t.WalletId, t.Type.ToString(), t.Amount,
            t.BalanceAfter, t.RazorpayOrderId, t.RazorpayPaymentId, t.Status.ToString(),
            t.SaleTransactionId, t.Description, t.CreatedAt)).ToList();
    }
}
