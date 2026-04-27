using System;
using System.Threading;
using System.Threading.Tasks;
using SalesService.Domain.Entities;

namespace SalesService.Domain.Interfaces
{
    public interface IFuelPreAuthorizationRepository
    {
        Task<FuelPreAuthorization?> GetByAuthCodeAsync(string authCode, CancellationToken ct = default);
        Task<FuelPreAuthorization> AddAsync(FuelPreAuthorization preAuth, CancellationToken ct = default);
        Task UpdateAsync(FuelPreAuthorization preAuth, CancellationToken ct = default);
    }
}
