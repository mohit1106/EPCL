using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository userRepo,
    IOtpRepository otpRepo,
    ILogger<ResetPasswordCommandHandler> logger)
    : IRequestHandler<ResetPasswordCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct)
            ?? throw new NotFoundException("User", cmd.Email);

        // Validate OTP
        var otp = await otpRepo.GetLatestValidOtpAsync(user.Id, OtpPurpose.PasswordReset, ct)
            ?? throw new DomainException("Invalid or expired OTP.");

        if (otp.OtpCode != cmd.OtpCode)
            throw new DomainException("Invalid OTP code.");

        if (!otp.IsValid)
            throw new DomainException("OTP has expired. Please request a new one.");

        // Mark OTP as used
        otp.MarkUsed();
        await otpRepo.UpdateAsync(otp, ct);

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword, workFactor: 12);
        user.ResetFailedLoginAttempts();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userRepo.UpdateAsync(user, ct);

        logger.LogInformation("Password reset successfully for UserId: {UserId}", user.Id);

        return new MessageResponseDto("Password has been reset successfully.");
    }
}
