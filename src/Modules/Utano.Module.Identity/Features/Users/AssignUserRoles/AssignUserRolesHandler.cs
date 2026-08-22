using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Features.Users.AssignUserRoles;

public class AssignUserRolesHandler(
    IdentityDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService,
    ILogger<AssignUserRolesHandler> logger)
    : IRequestHandler<AssignUserRolesCommand>
{
    public async Task Handle(AssignUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command.RoleIds.Count == 0)
            throw new UtanoDomainException("At least one role must be assigned.");

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == command.UserId && u.PracticeId == currentUser.PracticeId,
            cancellationToken);

        if (user is null)
            throw new UtanoDomainException("User not found.");

        var roles = await db.Roles
            .Where(r => command.RoleIds.Contains(r.Id) && r.PracticeId == currentUser.PracticeId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (roles.Count != command.RoleIds.Count)
            throw new UtanoDomainException("One or more roles are invalid or inactive.");

        var existing = await db.UserRoles
            .Where(ur => ur.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        db.UserRoles.RemoveRange(existing);
        db.UserRoles.AddRange(command.RoleIds.Select(roleId => new UserRoleAssignment(command.UserId, roleId)));

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "RolesAssigned",
                $"Name: {user.FullName} · Roles: {string.Join(", ", roles.Select(r => r.Name))}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log role assignment for {UserId}", user.Id);
        }
    }
}
