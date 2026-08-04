using MediatR;
using Utano.Module.Core.Authorization;

namespace Utano.Module.Identity.Features.Roles.CreateRole;

public record CreateRoleCommand(string Name, string? Description, List<string> Permissions)
    : IRequest<Guid>, IRequirePermission
{
    public string Permission => "settings.roles";
}
