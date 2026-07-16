using MediatR;

namespace Utano.Module.Identity.Features.Users.AssignUserRoles;

public record AssignUserRolesCommand(Guid UserId, List<Guid> RoleIds) : IRequest;
