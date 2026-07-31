using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

public class UpdateRoleHandler(IdentityDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(
                r => r.Id == command.Id && r.PracticeId == currentUser.PracticeId,
                cancellationToken);

        if (role is null)
            throw new UtanoDomainException("Role not found.");

        var nameTaken = await db.Roles.AnyAsync(
            r => r.PracticeId == currentUser.PracticeId && r.Name == command.Name && r.Id != command.Id,
            cancellationToken);

        if (nameTaken)
            throw new UtanoDomainException($"A role named '{command.Name}' already exists.");

        // System roles (Admin/Doctor/Nurse/...) are platform-owned: their name is relied on by
        // IModuleDescriptor.GetPermissionsForRole(roleName), and their permissions are meant to
        // come from that mapping, not be hand-edited per practice. Description and active-state
        // (except deactivating Admin) are still fine to change.
        if (role.IsSystem)
        {
            if (!string.Equals(command.Name.Trim(), role.Name, StringComparison.Ordinal))
                throw new UtanoDomainException("System role names cannot be changed.");

            var currentPermissions = role.GetPermissionKeys().ToHashSet();
            var requestedPermissions = command.Permissions.ToHashSet();
            if (!currentPermissions.SetEquals(requestedPermissions))
                throw new UtanoDomainException(
                    "System role permissions are managed by the platform and cannot be edited directly.");

            if (role.Name == SystemRoles.Admin && !command.IsActive)
                throw new UtanoDomainException("The Admin role cannot be deactivated.");
        }

        role.Update(command.Name, command.Description);
        role.SetPermissions(command.Permissions);

        if (command.IsActive && !role.IsActive) role.Activate();
        else if (!command.IsActive && role.IsActive) role.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
