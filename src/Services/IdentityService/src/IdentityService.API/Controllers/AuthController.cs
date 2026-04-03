using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.Commands.ChangePassword;
using IdentityService.Application.Commands.ForgotPassword;
using IdentityService.Application.Commands.GoogleLogin;
using IdentityService.Application.Commands.LoginUser;
using IdentityService.Application.Commands.LogoutUser;
using IdentityService.Application.Commands.RefreshToken;
using IdentityService.Application.Commands.RegisterUser;
using IdentityService.Application.Commands.ResetPassword;
using IdentityService.Application.Commands.VerifyOtp;
using IdentityService.Application.DTOs;

namespace IdentityService.API.Controllers;

/// <summary>
/// Handles authentication operations — register, login, logout, password management, OTP.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await mediator.Send(new RegisterUserCommand(
            dto.FullName, dto.Email, dto.PhoneNumber,
            dto.Password, dto.ConfirmPassword, dto.Role,
            dto.StationLicenseNumber, dto.ReferralCode));
        return StatusCode(201, result);
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await mediator.Send(new LoginUserCommand(dto.Email, dto.Password));
        return Ok(result);
    }

    /// <summary>Login with Google OAuth ID token.</summary>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
    {
        var result = await mediator.Send(new GoogleLoginCommand(dto.IdToken));
        return Ok(result);
    }

    /// <summary>Refresh an access token using a refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await mediator.Send(new RefreshTokenCommand(dto.Token));
        return Ok(result);
    }

    /// <summary>Logout and revoke all refresh tokens.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        var result = await mediator.Send(new LogoutUserCommand(userId));
        return Ok(result);
    }

    /// <summary>Request a password reset OTP via email.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var result = await mediator.Send(new ForgotPasswordCommand(dto.Email));
        return Ok(result);
    }

    /// <summary>Reset password using OTP verification.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        var result = await mediator.Send(new ResetPasswordCommand(dto.Email, dto.OtpCode, dto.NewPassword));
        return Ok(result);
    }

    /// <summary>Change password for authenticated user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new ChangePasswordCommand(userId, dto.CurrentPassword, dto.NewPassword));
        return Ok(result);
    }

    /// <summary>Verify an OTP code.</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(VerifyOtpResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
    {
        var result = await mediator.Send(new VerifyOtpCommand(dto.Email, dto.OtpCode, dto.Purpose));
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return Guid.Parse(claim);
    }
}

/// <summary>DTO for refresh token request body.</summary>
public record RefreshTokenRequestDto(string Token);
