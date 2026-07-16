namespace Utano.Module.Identity.Features.Users.GetUsers;

public record UserSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status,
    List<Guid> RoleIds
);
