using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
    Task<User?> GetByGoogleSubAsync(string googleSub, CancellationToken ct = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, UserRole? role = null, bool? isActive = null,
        string? searchTerm = null, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default);
}
