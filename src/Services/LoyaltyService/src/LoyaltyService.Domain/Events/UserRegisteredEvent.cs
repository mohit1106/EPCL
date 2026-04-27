namespace LoyaltyService.Domain.Events;

public class UserRegisteredEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
