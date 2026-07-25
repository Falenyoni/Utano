using MediatR;
using Utano.Module.Core.Modules;

namespace Utano.Module.Identity.Features.Roles.GetPermissions;

public class GetPermissionsHandler(IEnumerable<IModuleDescriptor> moduleDescriptors)
    : IRequestHandler<GetPermissionsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetPermissionsQuery query, CancellationToken cancellationToken)
    {
        var all = moduleDescriptors
            .SelectMany(m => m.AllPermissions)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(all);
    }
}
