using MediatR;
using Utano.Module.Core.Authorization;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

public record UpdateRoleCommand(Guid Id, string Name, string? Description, List<string> Permissions, bool IsActive)
    : IRequest, IRequirePermission
{
    public string Permission => "settings.roles";
}
