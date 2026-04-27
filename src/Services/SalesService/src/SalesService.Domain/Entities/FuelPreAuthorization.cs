using System;

namespace SalesService.Domain.Entities
{
    public class FuelPreAuthorization
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DriverUserId { get; set; }
        public Guid FleetAccountId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid StationId { get; set; }
        public decimal AuthorizedAmountINR { get; set; }
        public decimal? AuthorizedLitres { get; set; }
        public Guid FuelTypeId { get; set; }
        public string AuthCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Used, Expired, Cancelled
        public DateTime ExpiresAt { get; set; }
        public Guid? UsedByTransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
