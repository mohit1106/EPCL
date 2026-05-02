namespace IdentityService.Application.DTOs;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 900;
    public UserDto User { get; set; } = null!;
}

public record RegisterRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    string Role,
    string? StationLicenseNumber = null,
    string? ReferralCode = null
);

public record GoogleLoginRequestDto(string IdToken);

public record ForgotPasswordRequestDto(string Email);

public record ResetPasswordRequestDto(string Email, string OtpCode, string NewPassword);

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

public record VerifyOtpRequestDto(string Email, string OtpCode, string Purpose);

public record UpdateUserRoleRequestDto(string Role);

public record LockUserRequestDto(bool IsLocked, string? Reason);

public record UpdateProfileRequestDto(
    string? FullName,
    string? City,
    string? State,
    string? PinCode,
    string? PreferredLanguage
);

public record UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public string AuthProvider { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public UserProfileDto? Profile { get; set; }
}

public record UserProfileDto
{
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public Guid? StationId { get; set; }
    public string PreferredLanguage { get; set; } = "en";
}

public record MessageResponseDto(string Message);
public record RegisterResponseDto(Guid UserId, string Message);
public record VerifyOtpResponseDto(bool IsValid, string Message);

/// <summary>Minimal admin info for dealer contact feature.</summary>
public record AdminSummaryDto(Guid Id, string FullName, string Email);

// ── Help Request DTOs ──────────────────────────────────────────────

public record CreateHelpRequestDto(
    Guid? TargetAdminId,
    string TargetAdminName,
    string Category,
    string Message
);

public record HelpRequestReplyDto
{
    public Guid Id { get; set; }
    public string FromRole { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public Guid FromUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public record HelpRequestDto
{
    public Guid Id { get; set; }
    public Guid DealerUserId { get; set; }
    public string DealerEmail { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public Guid? TargetAdminId { get; set; }
    public string TargetAdminName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public List<HelpRequestReplyDto> Replies { get; set; } = new();
}

public record CreateHelpReplyDto(string Message);

public record UpdateHelpRequestStatusDto(string Status);
