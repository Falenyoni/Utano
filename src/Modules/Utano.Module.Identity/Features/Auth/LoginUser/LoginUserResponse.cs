namespace Utano.Module.Identity.Features.Auth.LoginUser;

public record LoginUserResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    List<string> Roles,
    List<string> Permissions,
    Guid PracticeId,
    string PracticeName,
    string? PrimaryColor,
    string? LogoBase64,
    bool HasDispensary,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
