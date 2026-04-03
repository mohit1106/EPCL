namespace StationService.Domain.Entities;

public class Station
{
    public Guid Id { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public Guid DealerUserId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public TimeOnly OperatingHoursStart { get; set; }
    public TimeOnly OperatingHoursEnd { get; set; }
    public bool Is24x7 { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<FuelType> FuelTypes { get; set; } = new List<FuelType>();
}
