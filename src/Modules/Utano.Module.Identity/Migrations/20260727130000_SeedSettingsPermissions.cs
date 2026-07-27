using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260727130000_SeedSettingsPermissions")]
    public partial class SeedSettingsPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove old catch-all permission
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions"" WHERE ""PermissionKey"" = 'settings.users';
");

            // Seed granular settings permissions for Admin
            migrationBuilder.Sql(@"
WITH new_perms (permission) AS (
    VALUES
        ('settings.users.view'),
        ('settings.users.manage'),
        ('settings.roles'),
        ('settings.staff.view'),
        ('settings.staff.manage'),
        ('settings.practice'),
        ('settings.billing_config.view'),
        ('settings.billing_config.manage')
)
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", np.permission
FROM identity.""Roles"" r
JOIN new_perms np ON true
WHERE r.""Name"" = 'Admin'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = np.permission
);
");

            // Seed billing config view for Billing role
            migrationBuilder.Sql(@"
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", 'settings.billing_config.view'
FROM identity.""Roles"" r
WHERE r.""Name"" = 'Billing'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = 'settings.billing_config.view'
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions""
WHERE ""PermissionKey"" IN (
    'settings.users.view', 'settings.users.manage',
    'settings.roles',
    'settings.staff.view', 'settings.staff.manage',
    'settings.practice',
    'settings.billing_config.view', 'settings.billing_config.manage'
);
");
            // Restore old permission for Admin
            migrationBuilder.Sql(@"
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", 'settings.users'
FROM identity.""Roles"" r
WHERE r.""Name"" = 'Admin'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = 'settings.users'
);
");
        }
    }
}
