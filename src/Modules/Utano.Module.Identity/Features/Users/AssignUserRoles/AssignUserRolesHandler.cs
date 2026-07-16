using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Features.Users.AssignUserRoles;

public class AssignUserRolesHandler(IdentityDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AssignUserRolesCommand>
{
    public async Task Handle(AssignUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command.RoleIds.Count == 0)
            throw new UtanoDomainException("At least one role must be assigned.");

        var userExists = await db.Users.AnyAsync(
            u => u.Id == command.UserId && u.PracticeId == currentUser.PracticeId,
            cancellationToken);

        if (!userExists)
            throw new UtanoDomainException("User not found.");

        var validRoleCount = await db.Roles.CountAsync(
            r => command.RoleIds.Contains(r.Id) && r.PracticeId == currentUser.PracticeId && r.IsActive,
            cancellationToken);

        if (validRoleCount != command.RoleIds.Count)
            throw new UtanoDomainException("One or more roles are invalid or inactive.");

        var existing = await db.UserRoles
            .Where(ur => ur.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        db.UserRoles.RemoveRange(existing);
        db.UserRoles.AddRange(command.RoleIds.Select(roleId => new UserRoleAssignment(command.UserId, roleId)));

        await db.SaveChangesAsync(cancellationToken);
    }
}
