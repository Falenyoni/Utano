using MediatR;

namespace Utano.Module.Identity.Features.Roles.GetPermissions;

public record GetPermissionsQuery : IRequest<IReadOnlyList<string>>;
