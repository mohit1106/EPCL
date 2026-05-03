namespace SalesService.Application.DTOs;

// ── Core DTOs ──────────────────────────────────────────────────────
public record TransactionDto(
    Guid Id, string ReceiptNumber, Guid StationId, Guid PumpId, Guid FuelTypeId,
    Guid DealerUserId, Guid? CustomerUserId, string VehicleNumber,
    decimal QuantityLitres, decimal PricePerLitre, decimal TotalAmount,
    string PaymentMethod, string? PaymentReferenceId, string Status,
    string FraudCheckStatus, int LoyaltyPointsEarned, int LoyaltyPointsRedeemed,
    DateTimeOffset Timestamp, bool IsVoided);

public record PumpDto(
    Guid Id, Guid StationId, Guid FuelTypeId, string PumpName, int NozzleCount,
    string Status, DateTimeOffset? LastServiced, DateTimeOffset? NextServiceDue, DateTimeOffset CreatedAt);

public record FuelPriceDto(
    Guid Id, Guid FuelTypeId, decimal PricePerLitre, DateTimeOffset EffectiveFrom,
    bool IsActive, Guid SetByUserId, DateTimeOffset CreatedAt);

public record ShiftDto(
    Guid Id, Guid DealerUserId, Guid StationId, DateTimeOffset StartedAt, DateTimeOffset? EndedAt,
    string OpeningStockJson, string? ClosingStockJson,
    decimal TotalLitresSold, decimal TotalRevenue, int TotalTransactions, bool DiscrepancyFlagged);

public record VehicleDto(
    Guid Id, Guid CustomerId, string RegistrationNumber, Guid? FuelTypePreference,
    string VehicleType, string? Nickname, bool IsActive, DateTimeOffset RegisteredAt);

public record FleetAccountDto(
    Guid Id, string CompanyName, Guid ContactUserId, decimal CreditLimit,
    decimal CurrentBalance, bool IsActive, DateTimeOffset CreatedAt);

public record FleetVehicleDto(Guid Id, Guid FleetAccountId, Guid VehicleId,
    decimal? DailyLimitLitres, decimal? MonthlyLimitAmount, bool IsActive);

public record WalletDto(Guid Id, Guid CustomerId, decimal Balance, decimal TotalLoaded, bool IsActive);

public record WalletTransactionDto(
    Guid Id, Guid WalletId, string Type, decimal Amount, decimal BalanceAfter,
    string? RazorpayOrderId, string? RazorpayPaymentId, string Status,
    Guid? SaleTransactionId, string? Description, DateTimeOffset CreatedAt);

public record MessageResponseDto(string Message);

// ── Request DTOs ───────────────────────────────────────────────────
public record RecordFuelSaleRequest(
    Guid StationId, Guid PumpId, Guid TankId, Guid FuelTypeId,
    Guid? CustomerUserId, string VehicleNumber, decimal QuantityLitres,
    string PaymentMethod, string? PaymentReferenceId);

public record VoidTransactionRequest(string Reason);

public record RegisterPumpRequest(Guid StationId, Guid FuelTypeId, string PumpName, int NozzleCount);
public record UpdatePumpStatusRequest(string Status, string? Notes);

public record SetFuelPriceRequest(Guid FuelTypeId, decimal PricePerLitre, DateTimeOffset EffectiveFrom);

public record StartShiftRequest(Guid StationId, string? Notes);
public record EndShiftRequest(string? Notes);

public record RegisterVehicleRequest(
    string RegistrationNumber, Guid? FuelTypePreference, string VehicleType, string? Nickname);

public record CreateFleetAccountRequest(string CompanyName, Guid ContactUserId, decimal CreditLimit);
public record AddFleetVehicleRequest(Guid VehicleId, decimal? DailyLimitLitres, decimal? MonthlyLimitAmount);

public record CreateWalletOrderRequest(decimal Amount);
public record VerifyWalletPaymentRequest(string OrderId, string PaymentId, string Signature);

public record CreateOrderResponseDto(string OrderId, decimal Amount, string Currency, string KeyId);

// ── Parking DTOs ───────────────────────────────────────────────────
public record ParkingSlotDto(
    Guid Id, Guid StationId, string SlotType, string SlotNumber, bool IsAvailable);

public record ParkingBookingDto(
    Guid Id, Guid ParkingSlotId, Guid StationId, Guid CustomerId,
    string SlotType, int DurationHours, decimal Amount, string Status,
    string? RazorpayOrderId, string? RazorpayPaymentId,
    DateTimeOffset BookedAt, DateTimeOffset ExpiresAt);

public record CreateParkingBookingRequest(Guid StationId, string SlotType, int DurationHours);
public record ConfirmParkingPaymentRequest(string OrderId, string PaymentId, string Signature);

// ── Wallet Payment Request DTOs ────────────────────────────────────
public record WalletPaymentRequestDto(
    Guid Id, Guid SaleTransactionId, Guid CustomerId, Guid DealerUserId,
    Guid StationId, decimal Amount, string Status, string Description,
    string? VehicleNumber, string? FuelTypeName, decimal? QuantityLitres,
    string PaymentMethod, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public record CreateWalletPaymentRequestDto(
    Guid SaleTransactionId, Guid CustomerId, decimal Amount,
    string Description, string? VehicleNumber, string? FuelTypeName, decimal? QuantityLitres,
    string? PaymentMethod);

// ── Daily Summary DTO ──────────────────────────────────────────────
public record DailySummaryDto(
    string Date, int TotalTransactions, decimal TotalLitres, decimal TotalRevenue,
    IReadOnlyList<HourlyDataDto> HourlyData);

public record HourlyDataDto(int Hour, int Transactions, decimal Litres, decimal Revenue);
