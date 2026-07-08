namespace Utano.Module.Identity.Features.Auth.RefreshToken;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
