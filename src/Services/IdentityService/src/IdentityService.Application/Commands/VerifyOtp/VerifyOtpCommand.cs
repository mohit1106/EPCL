using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.VerifyOtp;

public record VerifyOtpCommand(string Email, string OtpCode, string Purpose) : IRequest<VerifyOtpResponseDto>;
