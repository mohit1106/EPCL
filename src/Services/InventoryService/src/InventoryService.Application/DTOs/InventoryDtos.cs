namespace InventoryService.Application.DTOs;

public record TankDto(
    Guid Id, Guid StationId, Guid FuelTypeId, string TankSerialNumber,
    decimal CapacityLitres, decimal CurrentStockLitres, decimal ReservedLitres,
    decimal AvailableStock, decimal MinThresholdLitres, string Status,
    DateTimeOffset? LastReplenishedAt, DateTimeOffset? LastDipReadingAt,
    DateTimeOffset CreatedAt);

public record StockLoadingDto(
    Guid Id, Guid TankId, decimal QuantityLoadedLitres, Guid LoadedByUserId,
    string TankerNumber, string InvoiceNumber, string? SupplierName,
    decimal StockBefore, decimal StockAfter, DateTimeOffset Timestamp, string? Notes);

public record DipReadingDto(
    Guid Id, Guid TankId, decimal DipValueLitres, decimal SystemStockLitres,
    decimal VarianceLitres, decimal VariancePercent, bool IsFraudFlagged,
    Guid RecordedByUserId, DateTimeOffset Timestamp, string? Notes);

public record ReplenishmentRequestDto(
    Guid Id, Guid StationId, Guid TankId, Guid RequestedByUserId,
    decimal RequestedQuantityLitres, string UrgencyLevel, string Status,
    DateTimeOffset RequestedAt, Guid? ReviewedByUserId, DateTimeOffset? ReviewedAt,
    string? RejectionReason, string? Notes,
    // Extended fields
    string OrderNumber, string? TargetPumpName, string? FuelTypeName,
    string Priority, string? RequestedWindow,
    // Driver
    Guid? AssignedDriverId, string? AssignedDriverName,
    string? AssignedDriverPhone, string? AssignedDriverCode,
    // Verification
    DateTimeOffset? DealerVerifiedAt, string? DealerVerifiedDriverCode);

public record StockSummaryDto(
    int TotalTanks, decimal TotalCapacity, decimal TotalCurrentStock,
    decimal TotalReserved, int LowStockTanks, int CriticalTanks, int OutOfStockTanks);

// ── Request DTOs ────────────────────────────────────────────────────

public record AddTankRequest(
    Guid StationId, Guid FuelTypeId, string TankSerialNumber,
    decimal CapacityLitres, decimal CurrentStockLitres, decimal MinThresholdLitres);

public record UpdateTankRequest(
    decimal? CapacityLitres, decimal? MinThresholdLitres, string? Status);

public record RecordStockLoadingRequest(
    Guid TankId, decimal QuantityLoadedLitres, string TankerNumber,
    string InvoiceNumber, string? SupplierName, string? Notes);

public record RecordDipReadingRequest(Guid TankId, decimal DipValueLitres, string? Notes);

public record SubmitReplenishmentRequest(
    Guid StationId, Guid TankId, decimal RequestedQuantityLitres,
    string UrgencyLevel, string? Notes,
    string? TargetPumpName, string? FuelTypeName,
    string? Priority, string? RequestedWindow);

public record ReviewReplenishmentRequest(string? Notes);
public record RejectReplenishmentRequest(string Reason);

public record AssignDriverRequest(
    Guid DriverId, string DriverName, string DriverPhone, string DriverCode);

public record UpdateReplenishmentStatusRequest(string Status);

public record VerifyOffloadingRequest(string OrderNumber, string DriverCode);

public record MessageResponseDto(string Message);
