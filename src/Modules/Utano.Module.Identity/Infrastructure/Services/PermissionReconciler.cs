using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Core.Modules;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Infrastructure.Services;

// Runs once at startup. Additive-only by design: it only ever inserts a permission a role or the
// catalog doesn't already have - it never removes one, since there's no way to tell "never had it"
// apart from "removed on purpose" without extra bookkeeping that isn't being built. Deletions stay
// deliberate, reviewed migrations (same as the existing SeedSettingsPermissions-style migrations).
public static class PermissionReconciler
{
    public static async Task ReconcileAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var moduleDescriptors = scope.ServiceProvider.GetServices<IModuleDescriptor>().ToList();

        await ReconcileCatalogAsync(db, moduleDescriptors, ct);
        await ReconcileSystemRolePermissionsAsync(db, moduleDescriptors, ct);
    }

    // Keeps the Permissions catalog table (#5) in sync with whatever the code currently declares,
    // so a permission introduced in a future code change doesn't need its own hand-written
    // migration just to exist in the catalog before anything can reference it.
    private static async Task ReconcileCatalogAsync(
        IdentityDbContext db, List<IModuleDescriptor> moduleDescriptors, CancellationToken ct)
    {
        var knownKeys = moduleDescriptors.SelectMany(m => m.AllPermissions).Distinct().ToList();
        var existingKeys = await db.Permissions.Select(p => p.Key).ToListAsync(ct);
        var missingKeys = knownKeys.Except(existingKeys);

        foreach (var key in missingKeys)
            db.Permissions.Add(new Permission(key));

        await db.SaveChangesAsync(ct);
    }

    // For every practice's system roles (Admin/Doctor/Nurse/...), grants whatever permissions
    // IModuleDescriptor.GetPermissionsForRole(roleName) says that role should have but doesn't
    // yet - covers practices created before a permission existed in code. Matched on
    // (PracticeId, Name, IsSystem=true), the same reliable key used elsewhere since renaming
    // system roles is blocked at the API layer.
    private static async Task ReconcileSystemRolePermissionsAsync(
        IdentityDbContext db, List<IModuleDescriptor> moduleDescriptors, CancellationToken ct)
    {
        var systemRoles = await db.Roles
            .Include(r => r.Permissions)
            .Where(r => r.IsSystem)
            .ToListAsync(ct);

        foreach (var role in systemRoles)
        {
            var expected = moduleDescriptors
                .SelectMany(m => m.GetPermissionsForRole(role.Name))
                .Distinct();

            var current = role.GetPermissionKeys().ToHashSet();
            var missing = expected.Where(p => !current.Contains(p));

            foreach (var permission in missing)
                db.RolePermissions.Add(new RolePermission(role.Id, permission));
        }

        await db.SaveChangesAsync(ct);
    }
}
