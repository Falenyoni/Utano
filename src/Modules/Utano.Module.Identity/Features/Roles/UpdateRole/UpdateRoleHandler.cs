using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

public class UpdateRoleHandler(IdentityDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateRoleCommand>
{
    private const string ManageRolesPermission = "settings.roles";

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

        // System role names are relied on by IModuleDescriptor.GetPermissionsForRole(roleName) and
        // any future reconciliation, so the name stays locked. Permissions are otherwise freely
        // editable per practice now - PermissionAuthorizationBehavior already confirmed the caller
        // holds settings.roles before this handler runs, so the gate belongs on who is acting, not
        // on which role is being touched.
        if (role.IsSystem && !string.Equals(command.Name.Trim(), role.Name, StringComparison.Ordinal))
            throw new UtanoDomainException("System role names cannot be changed.");

        // Invariant: a practice must never end up with zero active roles holding settings.roles -
        // otherwise nobody could ever manage roles/permissions again. Checks the real condition
        // (does any other active role grant it) rather than a hardcoded "is this Admin" check, so
        // it also protects a custom role that happens to be the practice's only role-manager.
        var willStillManageRoles = command.IsActive && command.Permissions.Contains(ManageRolesPermission);
        var currentlyManagesRoles = role.IsActive && role.GetPermissionKeys().Contains(ManageRolesPermission);

        if (currentlyManagesRoles && !willStillManageRoles)
        {
            var otherActiveRoles = await db.Roles
                .Include(r => r.Permissions)
                .Where(r => r.PracticeId == currentUser.PracticeId && r.Id != role.Id && r.IsActive)
                .ToListAsync(cancellationToken);

            var stillHasAManager = otherActiveRoles.Any(r => r.GetPermissionKeys().Contains(ManageRolesPermission));

            if (!stillHasAManager)
                throw new UtanoDomainException(
                    "This is the practice's only active role that can manage roles and permissions. Leave at least one other active role with 'settings.roles' before deactivating this one or removing that permission from it.");
        }

        role.Update(command.Name, command.Description);
        role.SetPermissions(command.Permissions);

        if (command.IsActive && !role.IsActive) role.Activate();
        else if (!command.IsActive && role.IsActive) role.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
