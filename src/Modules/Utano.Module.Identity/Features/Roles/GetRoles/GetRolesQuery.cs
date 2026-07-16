using MediatR;

namespace Utano.Module.Identity.Features.Roles.GetRoles;

public record GetRolesQuery : IRequest<List<RoleRow>>;

public record RoleRow(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    List<string> Permissions);
