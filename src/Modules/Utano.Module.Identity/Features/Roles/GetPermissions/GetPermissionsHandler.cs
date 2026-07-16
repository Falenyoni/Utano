using MediatR;
using Utano.Module.Identity.Domain.Constants;

namespace Utano.Module.Identity.Features.Roles.GetPermissions;

public class GetPermissionsHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetPermissionsQuery query, CancellationToken cancellationToken)
        => Task.FromResult(Permissions.All);
}
