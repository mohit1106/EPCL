namespace IdentityService.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? AadhaarLast4 { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public Guid? StationId { get; set; }
    public string PreferredLanguage { get; set; } = "en";

    // Navigation property
    public User User { get; set; } = null!;
}
