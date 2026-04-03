namespace InventoryService.Domain.Enums;

public enum TankStatus
{
    Available,
    Low,
    Critical,
    OutOfStock,
    Replenishing,
    Reconciling,
    OutOfService
}

public enum ReplenishmentStatus
{
    Submitted,
    UnderReview,
    Approved,
    Dispatched,
    Delivered,
    Rejected
}

public enum UrgencyLevel
{
    Low,
    Normal,
    High,
    Critical
}
