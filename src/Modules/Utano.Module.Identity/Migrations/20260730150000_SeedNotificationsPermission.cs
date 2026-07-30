using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260730150000_SeedNotificationsPermission")]
    public partial class SeedNotificationsPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // notifications.view is granted to every role - it only ever gates a user's own
            // notifications, never anyone else's data, so there's nothing to restrict per role.
            migrationBuilder.Sql(@"
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", 'notifications.view'
FROM identity.""Roles"" r
WHERE NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = 'notifications.view'
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions"" WHERE ""PermissionKey"" = 'notifications.view';
");
        }
    }
}
