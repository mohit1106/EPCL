using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.VerifyOtp;

public class VerifyOtpCommandHandler(
    IUserRepository userRepo,
    IOtpRepository otpRepo,
    ILogger<VerifyOtpCommandHandler> logger)
    : IRequestHandler<VerifyOtpCommand, VerifyOtpResponseDto>
{
    public async Task<VerifyOtpResponseDto> Handle(VerifyOtpCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct)
            ?? throw new NotFoundException("User", cmd.Email);

        if (!Enum.TryParse<OtpPurpose>(cmd.Purpose, true, out var purpose))
            throw new DomainException($"Invalid OTP purpose: {cmd.Purpose}");

        var otp = await otpRepo.GetLatestValidOtpAsync(user.Id, purpose, ct);

        if (otp == null || otp.OtpCode != cmd.OtpCode || !otp.IsValid)
        {
            logger.LogWarning("Invalid OTP verification attempt. UserId: {UserId}, Purpose: {Purpose}", user.Id, cmd.Purpose);
            return new VerifyOtpResponseDto(false, "Invalid or expired OTP.");
        }

        otp.MarkUsed();
        await otpRepo.UpdateAsync(otp, ct);

        logger.LogInformation("OTP verified successfully. UserId: {UserId}, Purpose: {Purpose}", user.Id, cmd.Purpose);
        return new VerifyOtpResponseDto(true, "OTP verified successfully.");
    }
}
