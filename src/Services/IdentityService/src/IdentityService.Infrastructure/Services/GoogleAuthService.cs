using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IdentityService.Application.Interfaces;

namespace IdentityService.Infrastructure.Services;

public class GoogleAuthService(
    IConfiguration config,
    ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [config["GOOGLE_CLIENT_ID"]!]
                });

            return new GoogleUserPayload(
                Subject: payload.Subject,
                Email: payload.Email,
                Name: payload.Name,
                PictureUrl: payload.Picture
            );
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google ID token received");
            throw new Domain.Exceptions.DomainException("Invalid Google authentication token.");
        }
    }
}
