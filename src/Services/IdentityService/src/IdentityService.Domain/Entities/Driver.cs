namespace IdentityService.Domain.Entities;

/// <summary>
/// Delivery driver managed by admins. Not a system user — a standalone record
/// for fuel tanker drivers assigned to replenishment requests.
/// </summary>
public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable code shown to dealers during offloading verification (e.g., DRV-A1B2).</summary>
    public string DriverCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>False when assigned to an active (non-complete) replenishment request.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>The replenishment request this driver is currently assigned to (null if available).</summary>
    public Guid? CurrentRequestId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
