using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepo,
    IOtpRepository otpRepo,
    IEmailService emailService,
    IEmailTemplateService templateService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);

        // Return success even if user not found (prevent email enumeration)
        if (user == null)
        {
            logger.LogWarning("Forgot password requested for non-existent email: {Email}", cmd.Email);
            return new MessageResponseDto("If this email is registered, you will receive a password reset code.");
        }

        // Invalidate any existing OTPs
        await otpRepo.InvalidateAllForUserAsync(user.Id, OtpPurpose.PasswordReset, ct);

        // Generate 6-digit OTP
        var otpCode = Random.Shared.Next(100000, 999999).ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        var otpRequest = new OtpRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OtpCode = otpCode,
            Purpose = OtpPurpose.PasswordReset,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await otpRepo.AddAsync(otpRequest, ct);

        // Send OTP email
        var html = templateService.Render("otp", new Dictionary<string, string>
        {
            ["RecipientName"] = user.FullName,
            ["OtpCode"] = otpCode,
            ["Purpose"] = "reset your password",
            ["ExpiryTime"] = expiresAt.ToOffset(TimeSpan.FromHours(5.5)).ToString("hh:mm tt"),
            ["SupportUrl"] = "https://epcl.in/support"
        });

        await emailService.SendAsync(new EmailMessage(
            To: [new EmailRecipient(user.FullName, user.Email)],
            Subject: $"Your EPCL Verification Code — {otpCode}",
            HtmlBody: html,
            TextBody: $"Your EPCL OTP is: {otpCode}. Expires in 10 minutes."
        ), ct);

        logger.LogInformation("Password reset OTP sent. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return new MessageResponseDto("If this email is registered, you will receive a password reset code.");
    }
}
