namespace StationService.Application.DTOs;

public record StationDto
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
    public string OperatingHoursStart { get; set; } = string.Empty;
    public string OperatingHoursEnd { get; set; } = string.Empty;
    public bool Is24x7 { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public double? DistanceKm { get; set; }  // Populated only for nearby queries
}

public record FuelTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public record CreateStationRequest(
    string StationCode, string StationName, Guid DealerUserId,
    string AddressLine1, string City, string State, string PinCode,
    decimal Latitude, decimal Longitude, string LicenseNumber,
    string OperatingHoursStart, string OperatingHoursEnd, bool Is24x7);

public record UpdateStationRequest(
    string? StationName, string? AddressLine1, string? City,
    string? State, string? PinCode, decimal? Latitude,
    decimal? Longitude, string? OperatingHoursStart,
    string? OperatingHoursEnd, bool? Is24x7);

public record AssignDealerRequest(Guid DealerUserId);

public record CreateFuelTypeRequest(string Name, string? Description);
public record UpdateFuelTypeRequest(string? Name, string? Description, bool? IsActive);

public record MessageResponseDto(string Message);
