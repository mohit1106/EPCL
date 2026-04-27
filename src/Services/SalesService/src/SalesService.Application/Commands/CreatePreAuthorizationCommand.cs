using System;
using MediatR;
using SalesService.Domain.Entities;

namespace SalesService.Application.Commands
{
    public class CreatePreAuthorizationCommand : IRequest<FuelPreAuthorization>
    {
        public Guid DriverUserId { get; set; }
        public Guid FleetAccountId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid StationId { get; set; }
        public Guid FuelTypeId { get; set; }
        public decimal RequestedAmountINR { get; set; }

        public CreatePreAuthorizationCommand(Guid driverUserId, Guid fleetAccountId, Guid vehicleId, Guid stationId, Guid fuelTypeId, decimal requestedAmountINR)
        {
            DriverUserId = driverUserId;
            FleetAccountId = fleetAccountId;
            VehicleId = vehicleId;
            StationId = stationId;
            FuelTypeId = fuelTypeId;
            RequestedAmountINR = requestedAmountINR;
        }
    }
}
