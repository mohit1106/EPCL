namespace SalesService.Domain.Enums;

public enum TransactionStatus
{
    Initiated,
    StockReserved,
    Completed,
    Voided,
    FraudFlagged,
    FraudCleared
}

public enum FraudCheckStatus
{
    Pending,
    Cleared,
    Flagged
}

public enum PumpStatus
{
    Active,
    UnderMaintenance,
    OutOfService,
    Paused
}

public enum PaymentMethod
{
    Cash,
    UPI,
    Card,
    FleetCard,
    Wallet
}

public enum WalletTransactionType
{
    TopUp,
    Debit,
    Refund
}

public enum WalletTransactionStatus
{
    Pending,
    Captured,
    Failed,
    Refunded
}

public enum VehicleType
{
    TwoWheeler,
    FourWheeler,
    Commercial,
    CNG,
    Car,
    Motorcycle,
    Truck,
    Bus,
    Van,
    AutoRickshaw
}

public enum ParkingSlotType
{
    TwoWheeler,
    FourWheeler,
    HGV
}

public enum ParkingBookingStatus
{
    Initiated,
    Confirmed,
    Cancelled,
    Expired
}
