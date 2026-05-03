using Microsoft.EntityFrameworkCore;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.Infrastructure.Repositories;

public class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<User?> GetByGoogleSubAsync(string googleSub, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.GoogleSub == googleSub, ct);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, UserRole? role = null, bool? isActive = null,
        string? searchTerm = null, CancellationToken ct = default)
    {
        var query = context.Users.Include(u => u.Profile).AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.PhoneNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await context.Users.AddAsync(user, ct);
        await context.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return await context.Users.AnyAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
    }

    public async Task<UserStatsResult> GetUserStatsAsync(CancellationToken ct = default)
    {
        var users = context.Users.AsQueryable();

        var totalCount = await users.CountAsync(ct);
        var activeCount = await users.CountAsync(u => u.IsActive && u.IsEmailVerified, ct);
        var lockedCount = await users.CountAsync(u => !u.IsActive, ct);
        var pendingCount = await users.CountAsync(u => u.IsActive && !u.IsEmailVerified, ct);

        var customerCount = await users.CountAsync(u => u.Role == UserRole.Customer, ct);
        var dealerCount = await users.CountAsync(u => u.Role == UserRole.Dealer, ct);
        var adminCount = await users.CountAsync(u => u.Role == UserRole.Admin, ct);
        var superAdminCount = await users.CountAsync(u => u.Role == UserRole.SuperAdmin, ct);

        return new UserStatsResult(
            totalCount, activeCount, lockedCount, pendingCount,
            customerCount, dealerCount, adminCount, superAdminCount
        );
    }
}
