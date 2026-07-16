using MediatR;

namespace Utano.Module.Identity.Features.Roles.CreateRole;

public record CreateRoleCommand(string Name, string? Description, List<string> Permissions) : IRequest<Guid>;
