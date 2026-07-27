using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260727110000_SeedTriageCreatePermission")]
    public partial class SeedTriageCreatePermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
WITH new_perms (role_name, permission) AS (
    VALUES
        ('Admin',   'triage.create'),
        ('Doctor',  'triage.create'),
        ('Nurse',   'triage.create'),
        ('Triage',  'triage.create')
)
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", np.permission
FROM identity.""Roles"" r
JOIN new_perms np ON r.""Name"" = np.role_name
WHERE NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = np.permission
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions"" WHERE ""PermissionKey"" = 'triage.create';
");
        }
    }
}
