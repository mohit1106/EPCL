namespace IdentityService.Application.Interfaces;

public interface IGoogleAuthService
{
    Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}

public record GoogleUserPayload(
    string Subject,
    string Email,
    string Name,
    string? PictureUrl
);
