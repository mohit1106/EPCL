namespace IdentityService.Domain.Entities;

/// <summary>
/// Help request from a dealer to an admin.
/// </summary>
public class HelpRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DealerUserId { get; set; }
    public string DealerEmail { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public Guid? TargetAdminId { get; set; }
    public string TargetAdminName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Resolved, Dismissed
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    // Navigation
    public ICollection<HelpRequestReply> Replies { get; set; } = new List<HelpRequestReply>();
}

/// <summary>
/// A reply message on a help request (from either dealer or admin).
/// </summary>
public class HelpRequestReply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HelpRequestId { get; set; }
    public string FromRole { get; set; } = string.Empty; // "admin" or "dealer"
    public string FromName { get; set; } = string.Empty;
    public Guid FromUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public HelpRequest HelpRequest { get; set; } = null!;
}
