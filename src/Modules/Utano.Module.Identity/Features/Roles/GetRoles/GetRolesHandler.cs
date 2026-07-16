using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Roles.GetRoles;

public class GetRolesHandler(IdentityDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetRolesQuery, List<RoleRow>>
{
    public async Task<List<RoleRow>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => r.PracticeId == currentUser.PracticeId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleRow(
            r.Id,
            r.Name,
            r.Description,
            r.IsSystem,
            r.IsActive,
            r.GetPermissionKeys().OrderBy(p => p).ToList()
        )).ToList();
    }
}
