using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Events;
using InventoryService.Domain.Exceptions;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Application.Commands;

// ── AddTank ────────────────────────────────────────────────────────
public record AddTankCommand(
    Guid StationId, Guid FuelTypeId, string TankSerialNumber,
    decimal CapacityLitres, decimal CurrentStockLitres, decimal MinThresholdLitres
) : IRequest<TankDto>;

public class AddTankValidator : AbstractValidator<AddTankCommand>
{
    public AddTankValidator()
    {
        RuleFor(x => x.StationId).NotEmpty();
        RuleFor(x => x.FuelTypeId).NotEmpty();
        RuleFor(x => x.TankSerialNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CapacityLitres).GreaterThan(0);
        RuleFor(x => x.CurrentStockLitres).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinThresholdLitres).GreaterThan(0);
    }
}

public class AddTankHandler(
    ITankRepository tankRepo, IRabbitMqPublisher publisher,
    ILogger<AddTankHandler> logger) : IRequestHandler<AddTankCommand, TankDto>
{
    public async Task<TankDto> Handle(AddTankCommand cmd, CancellationToken ct)
    {
        if (await tankRepo.ExistsBySerialAsync(cmd.TankSerialNumber, ct))
            throw new DuplicateEntityException("Tank", "serial number", cmd.TankSerialNumber);

        if (cmd.CurrentStockLitres > cmd.CapacityLitres)
            throw new InsufficientCapacityException(0, cmd.CurrentStockLitres, cmd.CapacityLitres);

        var tank = new Tank
        {
            Id = Guid.NewGuid(),
            StationId = cmd.StationId,
            FuelTypeId = cmd.FuelTypeId,
            TankSerialNumber = cmd.TankSerialNumber,
            CapacityLitres = cmd.CapacityLitres,
            CurrentStockLitres = cmd.CurrentStockLitres,
            MinThresholdLitres = cmd.MinThresholdLitres,
            Status = TankStatus.Available,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await tankRepo.AddAsync(tank, ct);
        logger.LogInformation("Tank added. Id: {TankId}, Station: {StationId}", tank.Id, tank.StationId);

        return new TankDto(tank.Id, tank.StationId, tank.FuelTypeId, tank.TankSerialNumber,
            tank.CapacityLitres, tank.CurrentStockLitres, tank.ReservedLitres, tank.AvailableStock,
            tank.MinThresholdLitres, tank.Status.ToString(), tank.LastReplenishedAt,
            tank.LastDipReadingAt, tank.CreatedAt);
    }
}

// ── UpdateTank ─────────────────────────────────────────────────────
public record UpdateTankCommand(Guid TankId, decimal? CapacityLitres, decimal? MinThresholdLitres, string? Status) : IRequest<TankDto>;

public class UpdateTankHandler(ITankRepository tankRepo, ILogger<UpdateTankHandler> logger)
    : IRequestHandler<UpdateTankCommand, TankDto>
{
    public async Task<TankDto> Handle(UpdateTankCommand cmd, CancellationToken ct)
    {
        var tank = await tankRepo.GetByIdAsync(cmd.TankId, ct)
            ?? throw new NotFoundException("Tank", cmd.TankId);

        if (cmd.CapacityLitres.HasValue) tank.CapacityLitres = cmd.CapacityLitres.Value;
        if (cmd.MinThresholdLitres.HasValue) tank.MinThresholdLitres = cmd.MinThresholdLitres.Value;
        if (cmd.Status != null && Enum.TryParse<TankStatus>(cmd.Status, out var status)) tank.Status = status;
        tank.UpdatedAt = DateTimeOffset.UtcNow;

        await tankRepo.UpdateAsync(tank, ct);
        logger.LogInformation("Tank updated. Id: {TankId}", tank.Id);

        return new TankDto(tank.Id, tank.StationId, tank.FuelTypeId, tank.TankSerialNumber,
            tank.CapacityLitres, tank.CurrentStockLitres, tank.ReservedLitres, tank.AvailableStock,
            tank.MinThresholdLitres, tank.Status.ToString(), tank.LastReplenishedAt,
            tank.LastDipReadingAt, tank.CreatedAt);
    }
}

// ── RecordStockLoading (Rule 7.2: Capacity check + Invoice required) ──
public record RecordStockLoadingCommand(
    Guid TankId, decimal QuantityLoadedLitres, Guid LoadedByUserId,
    string TankerNumber, string InvoiceNumber, string? SupplierName, string? Notes
) : IRequest<StockLoadingDto>;

public class RecordStockLoadingValidator : AbstractValidator<RecordStockLoadingCommand>
{
    public RecordStockLoadingValidator()
    {
        RuleFor(x => x.TankId).NotEmpty();
        RuleFor(x => x.QuantityLoadedLitres).GreaterThan(0);
        RuleFor(x => x.TankerNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50)
            .WithMessage("InvoiceNumber is a regulatory requirement and must be provided.");
        RuleFor(x => x.LoadedByUserId).NotEmpty();
    }
}

public class RecordStockLoadingHandler(
    ITankRepository tankRepo, IStockLoadingRepository loadingRepo,
    IRabbitMqPublisher publisher, ILogger<RecordStockLoadingHandler> logger)
    : IRequestHandler<RecordStockLoadingCommand, StockLoadingDto>
{
    public async Task<StockLoadingDto> Handle(RecordStockLoadingCommand cmd, CancellationToken ct)
    {
        var tank = await tankRepo.GetByIdAsync(cmd.TankId, ct)
            ?? throw new NotFoundException("Tank", cmd.TankId);

        // BUSINESS RULE 7.2: Capacity check
        if (tank.CurrentStockLitres + cmd.QuantityLoadedLitres > tank.CapacityLitres)
            throw new InsufficientCapacityException(tank.CurrentStockLitres, cmd.QuantityLoadedLitres, tank.CapacityLitres);

        var stockBefore = tank.CurrentStockLitres;
        tank.CurrentStockLitres += cmd.QuantityLoadedLitres;
        tank.LastReplenishedAt = DateTimeOffset.UtcNow;
        tank.UpdatedAt = DateTimeOffset.UtcNow;

        // Update status if was Low/Critical/OutOfStock
        if (tank.CurrentStockLitres >= tank.MinThresholdLitres)
            tank.Status = TankStatus.Available;

        await tankRepo.UpdateAsync(tank, ct);

        var loading = new StockLoading
        {
            Id = Guid.NewGuid(),
            TankId = cmd.TankId,
            QuantityLoadedLitres = cmd.QuantityLoadedLitres,
            LoadedByUserId = cmd.LoadedByUserId,
            TankerNumber = cmd.TankerNumber,
            InvoiceNumber = cmd.InvoiceNumber,
            SupplierName = cmd.SupplierName,
            StockBefore = stockBefore,
            StockAfter = tank.CurrentStockLitres,
            Notes = cmd.Notes,
            Timestamp = DateTimeOffset.UtcNow
        };
        await loadingRepo.AddAsync(loading, ct);

        await publisher.PublishAsync(new FuelStockLoadedEvent
        {
            EventType = nameof(FuelStockLoadedEvent),
            TankId = tank.Id, StationId = tank.StationId,
            QuantityLoaded = cmd.QuantityLoadedLitres,
            InvoiceNumber = cmd.InvoiceNumber
        }, "inventory.stock.loaded", ct);

        logger.LogInformation("Stock loaded. Tank: {TankId}, Qty: {Qty}L, Before: {Before}L, After: {After}L",
            tank.Id, cmd.QuantityLoadedLitres, stockBefore, tank.CurrentStockLitres);

        return new StockLoadingDto(loading.Id, loading.TankId, loading.QuantityLoadedLitres,
            loading.LoadedByUserId, loading.TankerNumber, loading.InvoiceNumber,
            loading.SupplierName, loading.StockBefore, loading.StockAfter,
            loading.Timestamp, loading.Notes);
    }
}

// ── RecordDipReading (Rule 7.2: Variance > 2% → fraud flag) ────────
public record RecordDipReadingCommand(
    Guid TankId, decimal DipValueLitres, Guid RecordedByUserId, string? Notes
) : IRequest<DipReadingDto>;

public class RecordDipReadingHandler(
    ITankRepository tankRepo, IDipReadingRepository dipRepo,
    IRabbitMqPublisher publisher, ILogger<RecordDipReadingHandler> logger)
    : IRequestHandler<RecordDipReadingCommand, DipReadingDto>
{
    public async Task<DipReadingDto> Handle(RecordDipReadingCommand cmd, CancellationToken ct)
    {
        var tank = await tankRepo.GetByIdAsync(cmd.TankId, ct)
            ?? throw new NotFoundException("Tank", cmd.TankId);

        var systemStock = tank.CurrentStockLitres;
        var variance = cmd.DipValueLitres - systemStock;
        var variancePercent = systemStock > 0
            ? Math.Abs(variance / systemStock * 100)
            : 0;

        // BUSINESS RULE 7.2: Dip variance > 2% → fraud flag
        var isFraudFlagged = variancePercent > 2.0m;

        var reading = new DipReading
        {
            Id = Guid.NewGuid(),
            TankId = cmd.TankId,
            DipValueLitres = cmd.DipValueLitres,
            SystemStockLitres = systemStock,
            VarianceLitres = variance,
            VariancePercent = variancePercent,
            IsFraudFlagged = isFraudFlagged,
            RecordedByUserId = cmd.RecordedByUserId,
            Notes = cmd.Notes,
            Timestamp = DateTimeOffset.UtcNow
        };
        await dipRepo.AddAsync(reading, ct);

        tank.LastDipReadingAt = DateTimeOffset.UtcNow;
        await tankRepo.UpdateAsync(tank, ct);

        if (isFraudFlagged)
        {
            await publisher.PublishAsync(new DipVarianceDetectedEvent
            {
                EventType = nameof(DipVarianceDetectedEvent),
                TankId = tank.Id, StationId = tank.StationId,
                VariancePercent = variancePercent,
                DipValueLitres = cmd.DipValueLitres, SystemStockLitres = systemStock
            }, "inventory.dip.variance", ct);

            logger.LogWarning("FRAUD FLAG: Dip variance {Variance}% on Tank {TankId} (Station {StationId})",
                variancePercent, tank.Id, tank.StationId);
        }
        else
        {
            logger.LogInformation("Dip reading recorded. Tank: {TankId}, Dip: {Dip}L, System: {System}L, Variance: {Var}%",
                tank.Id, cmd.DipValueLitres, systemStock, variancePercent);
        }

        return new DipReadingDto(reading.Id, reading.TankId, reading.DipValueLitres,
            reading.SystemStockLitres, reading.VarianceLitres, reading.VariancePercent,
            reading.IsFraudFlagged, reading.RecordedByUserId, reading.Timestamp, reading.Notes);
    }
}

// ── Replenishment CRUD ─────────────────────────────────────────────
public record SubmitReplenishmentCommand(
    Guid StationId, Guid TankId, Guid RequestedByUserId,
    decimal RequestedQuantityLitres, string UrgencyLevel, string? Notes,
    string? TargetPumpName, string? FuelTypeName, string? Priority, string? RequestedWindow
) : IRequest<ReplenishmentRequestDto>;

public class SubmitReplenishmentHandler(
    ITankRepository tankRepo, IReplenishmentRequestRepository replRepo,
    IRabbitMqPublisher publisher, ILogger<SubmitReplenishmentHandler> logger)
    : IRequestHandler<SubmitReplenishmentCommand, ReplenishmentRequestDto>
{
    public async Task<ReplenishmentRequestDto> Handle(SubmitReplenishmentCommand cmd, CancellationToken ct)
    {
        var tank = await tankRepo.GetByIdAsync(cmd.TankId, ct)
            ?? throw new NotFoundException("Tank", cmd.TankId);

        if (tank.CurrentStockLitres + cmd.RequestedQuantityLitres > tank.CapacityLitres)
            throw new DomainException($"Cannot request {cmd.RequestedQuantityLitres}L. Exceeds tank capacity of {tank.CapacityLitres}L (Current stock: {tank.CurrentStockLitres}L).");

        Enum.TryParse<UrgencyLevel>(cmd.UrgencyLevel, out var urgency);

        var orderNumber = GenerateOrderNumber();
        var req = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            StationId = cmd.StationId,
            TankId = cmd.TankId,
            RequestedByUserId = cmd.RequestedByUserId,
            RequestedQuantityLitres = cmd.RequestedQuantityLitres,
            UrgencyLevel = urgency,
            Status = ReplenishmentStatus.Submitted,
            Notes = cmd.Notes,
            RequestedAt = DateTimeOffset.UtcNow,
            OrderNumber = orderNumber,
            TargetPumpName = cmd.TargetPumpName,
            FuelTypeName = cmd.FuelTypeName,
            Priority = cmd.Priority ?? "Standard",
            RequestedWindow = cmd.RequestedWindow,
        };
        await replRepo.AddAsync(req, ct);

        await publisher.PublishAsync(new ReplenishmentRequestedEvent
        {
            EventType = nameof(ReplenishmentRequestedEvent),
            RequestId = req.Id, StationId = req.StationId,
            TankId = req.TankId, UrgencyLevel = urgency.ToString(),
            RequestedByUserId = cmd.RequestedByUserId
        }, "inventory.replenishment.requested", ct);

        logger.LogInformation("Replenishment requested. Id: {Id}, Order: {Order}, Tank: {TankId}", req.Id, orderNumber, req.TankId);

        return MapToDto(req);
    }

    private static string GenerateOrderNumber()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        var code = new char[6];
        for (int i = 0; i < 6; i++) code[i] = chars[rng.Next(chars.Length)];
        return $"ORD-{new string(code)}";
    }

    private static ReplenishmentRequestDto MapToDto(ReplenishmentRequest r) => new(
        r.Id, r.StationId, r.TankId, r.RequestedByUserId,
        r.RequestedQuantityLitres, r.UrgencyLevel.ToString(), r.Status.ToString(),
        r.RequestedAt, r.ReviewedByUserId, r.ReviewedAt, r.RejectionReason, r.Notes,
        r.OrderNumber, r.TargetPumpName, r.FuelTypeName, r.Priority, r.RequestedWindow,
        r.AssignedDriverId, r.AssignedDriverName, r.AssignedDriverPhone, r.AssignedDriverCode,
        r.DealerVerifiedAt, r.DealerVerifiedDriverCode);
}

public record ApproveReplenishmentCommand(Guid RequestId, Guid ReviewedByUserId, string? Notes) : IRequest<MessageResponseDto>;

public class ApproveReplenishmentHandler(
    IReplenishmentRequestRepository replRepo, IRabbitMqPublisher publisher,
    ILogger<ApproveReplenishmentHandler> logger) : IRequestHandler<ApproveReplenishmentCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(ApproveReplenishmentCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);
        if (req.Status != ReplenishmentStatus.Submitted && req.Status != ReplenishmentStatus.UnderReview)
            throw new DomainException($"Cannot approve request in status '{req.Status}'.");

        req.Status = ReplenishmentStatus.Approved;
        req.ReviewedByUserId = cmd.ReviewedByUserId;
        req.ReviewedAt = DateTimeOffset.UtcNow;
        if (cmd.Notes != null) req.Notes = cmd.Notes;
        await replRepo.UpdateAsync(req, ct);

        await publisher.PublishAsync(new ReplenishmentApprovedEvent
        {
            EventType = nameof(ReplenishmentApprovedEvent),
            RequestId = req.Id, StationId = req.StationId,
            TankId = req.TankId, ApprovedByUserId = cmd.ReviewedByUserId
        }, "inventory.replenishment.approved", ct);

        logger.LogInformation("Replenishment approved. Id: {Id}", req.Id);
        return new MessageResponseDto("Replenishment request approved.");
    }
}

public record RejectReplenishmentCommand(Guid RequestId, Guid ReviewedByUserId, string Reason) : IRequest<MessageResponseDto>;

public class RejectReplenishmentHandler(
    IReplenishmentRequestRepository replRepo, ILogger<RejectReplenishmentHandler> logger)
    : IRequestHandler<RejectReplenishmentCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(RejectReplenishmentCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);

        req.Status = ReplenishmentStatus.Rejected;
        req.ReviewedByUserId = cmd.ReviewedByUserId;
        req.ReviewedAt = DateTimeOffset.UtcNow;
        req.RejectionReason = cmd.Reason;
        await replRepo.UpdateAsync(req, ct);

        logger.LogInformation("Replenishment rejected. Id: {Id}, Reason: {Reason}", req.Id, cmd.Reason);
        return new MessageResponseDto("Replenishment request rejected.");
    }
}

public record MarkDispatchedCommand(Guid RequestId) : IRequest<MessageResponseDto>;

public class MarkDispatchedHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<MarkDispatchedCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(MarkDispatchedCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);
        if (req.Status != ReplenishmentStatus.Approved)
            throw new DomainException($"Cannot mark as dispatched from status '{req.Status}'.");
        req.Status = ReplenishmentStatus.Dispatched;
        await replRepo.UpdateAsync(req, ct);
        return new MessageResponseDto("Replenishment marked as dispatched.");
    }
}

public record MarkDeliveredCommand(Guid RequestId) : IRequest<MessageResponseDto>;

public class MarkDeliveredHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<MarkDeliveredCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(MarkDeliveredCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);
        if (req.Status != ReplenishmentStatus.Dispatched)
            throw new DomainException($"Cannot mark as delivered from status '{req.Status}'.");
        req.Status = ReplenishmentStatus.Delivered;
        await replRepo.UpdateAsync(req, ct);
        return new MessageResponseDto("Replenishment marked as delivered.");
    }
}

// ── New Extended Commands ──────────────────────────────────────────

public record AssignDriverCommand(
    Guid RequestId, Guid DriverId, string DriverName, string DriverPhone, string DriverCode
) : IRequest<MessageResponseDto>;

public class AssignDriverHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<AssignDriverCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(AssignDriverCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);
        if (req.Status != ReplenishmentStatus.Approved)
            throw new DomainException($"Can only assign driver when status is 'Approved'. Current: '{req.Status}'.");

        req.AssignedDriverId = cmd.DriverId;
        req.AssignedDriverName = cmd.DriverName;
        req.AssignedDriverPhone = cmd.DriverPhone;
        req.AssignedDriverCode = cmd.DriverCode;
        req.Status = ReplenishmentStatus.TankerAssigned;
        await replRepo.UpdateAsync(req, ct);
        return new MessageResponseDto("Driver assigned and status updated to TankerAssigned.");
    }
}

public record UpdateReplenishmentStatusCommand(Guid RequestId, string NewStatus) : IRequest<MessageResponseDto>;

public class UpdateReplenishmentStatusHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<UpdateReplenishmentStatusCommand, MessageResponseDto>
{
    private static readonly Dictionary<ReplenishmentStatus, ReplenishmentStatus[]> ValidTransitions = new()
    {
        [ReplenishmentStatus.TankerAssigned] = [ReplenishmentStatus.InTransit],
        [ReplenishmentStatus.InTransit] = [ReplenishmentStatus.Offloading],
    };

    public async Task<MessageResponseDto> Handle(UpdateReplenishmentStatusCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);

        if (!Enum.TryParse<ReplenishmentStatus>(cmd.NewStatus, out var newStatus))
            throw new DomainException($"Invalid status: '{cmd.NewStatus}'.");

        if (!ValidTransitions.TryGetValue(req.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new DomainException($"Cannot transition from '{req.Status}' to '{newStatus}'.");

        req.Status = newStatus;
        await replRepo.UpdateAsync(req, ct);
        return new MessageResponseDto($"Status updated to {newStatus}.");
    }
}

public record VerifyOffloadingCommand(Guid RequestId, string OrderNumber, string DriverCode, Guid VerifiedByUserId) : IRequest<MessageResponseDto>;

public class VerifyOffloadingHandler(
    IReplenishmentRequestRepository replRepo, ITankRepository tankRepo,
    IStockLoadingRepository loadingRepo, ILogger<VerifyOffloadingHandler> logger)
    : IRequestHandler<VerifyOffloadingCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(VerifyOffloadingCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);

        if (req.Status != ReplenishmentStatus.Offloading)
            throw new DomainException($"Can only verify offloading when status is 'Offloading'. Current: '{req.Status}'.");

        if (!string.Equals(req.OrderNumber, cmd.OrderNumber, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Invalid Order Number.");

        if (!string.Equals(req.AssignedDriverCode, cmd.DriverCode, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Invalid Driver Code. Please ask the driver for their identification code.");

        // Record stock loading into the tank
        var tank = await tankRepo.GetByIdAsync(req.TankId, ct)
            ?? throw new NotFoundException("Tank", req.TankId);

        var stockBefore = tank.CurrentStockLitres;
        tank.CurrentStockLitres += req.RequestedQuantityLitres;
        if (tank.CurrentStockLitres > tank.CapacityLitres)
            tank.CurrentStockLitres = tank.CapacityLitres; // Cap at capacity
        tank.LastReplenishedAt = DateTimeOffset.UtcNow;
        tank.UpdatedAt = DateTimeOffset.UtcNow;
        if (tank.CurrentStockLitres >= tank.MinThresholdLitres)
            tank.Status = TankStatus.Available;
        await tankRepo.UpdateAsync(tank, ct);

        // Record stock loading entry
        var loading = new StockLoading
        {
            Id = Guid.NewGuid(),
            TankId = req.TankId,
            QuantityLoadedLitres = req.RequestedQuantityLitres,
            LoadedByUserId = cmd.VerifiedByUserId,
            TankerNumber = req.AssignedDriverName ?? "N/A",
            InvoiceNumber = req.OrderNumber,
            SupplierName = "EPCL Replenishment",
            StockBefore = stockBefore,
            StockAfter = tank.CurrentStockLitres,
            Notes = $"Verified offloading for order {req.OrderNumber}",
            Timestamp = DateTimeOffset.UtcNow
        };
        await loadingRepo.AddAsync(loading, ct);

        // Mark verification on request
        req.DealerVerifiedAt = DateTimeOffset.UtcNow;
        req.DealerVerifiedDriverCode = cmd.DriverCode;
        await replRepo.UpdateAsync(req, ct);

        logger.LogInformation("Offloading verified. Order: {Order}, Tank: {TankId}, Qty: {Qty}L",
            req.OrderNumber, req.TankId, req.RequestedQuantityLitres);

        return new MessageResponseDto($"Offloading verified. {req.RequestedQuantityLitres}L loaded into tank.");
    }
}

public record CompleteReplenishmentCommand(Guid RequestId) : IRequest<MessageResponseDto>;

public class CompleteReplenishmentHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<CompleteReplenishmentCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(CompleteReplenishmentCommand cmd, CancellationToken ct)
    {
        var req = await replRepo.GetByIdAsync(cmd.RequestId, ct)
            ?? throw new NotFoundException("ReplenishmentRequest", cmd.RequestId);

        if (req.Status != ReplenishmentStatus.Offloading)
            throw new DomainException($"Can only complete from 'Offloading' status. Current: '{req.Status}'.");

        if (req.DealerVerifiedAt == null)
            throw new DomainException("Cannot complete: dealer has not yet verified the offloading.");

        req.Status = ReplenishmentStatus.Complete;
        req.ReviewedAt = DateTimeOffset.UtcNow;
        await replRepo.UpdateAsync(req, ct);
        return new MessageResponseDto("Replenishment completed successfully.");
    }
}
