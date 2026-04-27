using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesService.Domain.Entities;
using SalesService.Domain.Interfaces;
using SalesService.Infrastructure.Persistence;

namespace SalesService.Infrastructure.Repositories
{
    public class FuelPreAuthorizationRepository : IFuelPreAuthorizationRepository
    {
        private readonly SalesDbContext _context;

        public FuelPreAuthorizationRepository(SalesDbContext context)
        {
            _context = context;
        }

        public async Task<FuelPreAuthorization> AddAsync(FuelPreAuthorization preAuth, CancellationToken ct = default)
        {
            await _context.FuelPreAuthorizations.AddAsync(preAuth, ct);
            await _context.SaveChangesAsync(ct);
            return preAuth;
        }

        public async Task<FuelPreAuthorization?> GetByAuthCodeAsync(string authCode, CancellationToken ct = default)
        {
            return await _context.FuelPreAuthorizations.FirstOrDefaultAsync(f => f.AuthCode == authCode, ct);
        }

        public async Task UpdateAsync(FuelPreAuthorization preAuth, CancellationToken ct = default)
        {
            _context.FuelPreAuthorizations.Update(preAuth);
            await _context.SaveChangesAsync(ct);
        }
    }
}
