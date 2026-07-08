namespace Utano.Module.Identity.Features.Auth.LoginUser;

public record LoginUserResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    Guid PracticeId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
