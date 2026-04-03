namespace IdentityService.Domain.Exceptions;

public class AccountLockedException : DomainException
{
    public DateTimeOffset? LockoutEnd { get; }

    public AccountLockedException(DateTimeOffset? lockoutEnd)
        : base($"Account is locked. Try again after {lockoutEnd?.ToString("yyyy-MM-dd HH:mm:ss")} UTC.")
    {
        LockoutEnd = lockoutEnd;
    }

    public AccountLockedException(string message) : base(message) { }
}
