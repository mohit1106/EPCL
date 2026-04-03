using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

public class OtpRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; }

    // Navigation property
    public User User { get; set; } = null!;

    // Domain methods
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
