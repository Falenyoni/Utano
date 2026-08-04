using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Utano.Module.Core.Services;

namespace Utano.API.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.Parse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? Guid.Empty.ToString());

    public string Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

    public string FullName =>
        User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public Guid PracticeId =>
        Guid.Parse(User?.FindFirstValue("PracticeId") ?? Guid.Empty.ToString());

    // Empty means an old token issued before this claim existed - callers should treat that as
    // "let it through," the same graceful-degradation SubscriptionMiddleware already uses for
    // SubscriptionStatus, since the claim will be populated again on the next login/refresh.
    public string SubscriptionTier =>
        User?.FindFirstValue("SubscriptionTier") ?? string.Empty;

    public IReadOnlyList<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToList() ?? [];

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission);
}
