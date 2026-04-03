namespace IdentityService.Domain.Enums;

public enum OtpPurpose
{
    PasswordReset = 0,
    MfaLogin = 1,
    PhoneVerify = 2,
    EmailVerify = 3
}
