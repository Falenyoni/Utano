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

    public IReadOnlyList<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToList() ?? [];

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission);
}
