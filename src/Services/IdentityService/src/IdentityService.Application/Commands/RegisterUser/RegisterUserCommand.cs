using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    string Role,
    string? StationLicenseNumber = null,
    string? ReferralCode = null
) : IRequest<RegisterResponseDto>;
