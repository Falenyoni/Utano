using MediatR;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

public record UpdateRoleCommand(Guid Id, string Name, string? Description, List<string> Permissions, bool IsActive) : IRequest;
