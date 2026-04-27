using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SalesService.Domain.Entities;
using SalesService.Domain.Interfaces;

namespace SalesService.Application.Commands
{
    public class CreatePreAuthorizationCommandHandler : IRequestHandler<CreatePreAuthorizationCommand, FuelPreAuthorization>
    {
        private readonly IFuelPreAuthorizationRepository _repository;

        public CreatePreAuthorizationCommandHandler(IFuelPreAuthorizationRepository repository)
        {
            _repository = repository;
        }

        public async Task<FuelPreAuthorization> Handle(CreatePreAuthorizationCommand request, CancellationToken cancellationToken)
        {
            var random = new Random();
            var authCode = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            var preAuth = new FuelPreAuthorization
            {
                DriverUserId = request.DriverUserId,
                FleetAccountId = request.FleetAccountId,
                VehicleId = request.VehicleId,
                StationId = request.StationId,
                FuelTypeId = request.FuelTypeId,
                AuthorizedAmountINR = request.RequestedAmountINR,
                AuthCode = authCode,
                Status = "Active",
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            await _repository.AddAsync(preAuth, cancellationToken);
            return preAuth;
        }
    }
}
