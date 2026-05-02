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
    TankerAssigned,
    InTransit,
    Offloading,
    Complete,
    Dispatched,    // Legacy — kept for backward compat
    Delivered,     // Legacy — kept for backward compat
    Rejected
}

public enum UrgencyLevel
{
    Low,
    Normal,
    High,
    Critical
}
