using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260726100000_SeedDispensaryAndClaimsPermissions")]
    public partial class SeedDispensaryAndClaimsPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // dispensary.view → Admin, Doctor, Nurse
            // dispensary.manage → Admin, Nurse
            // claims.view → Admin, Doctor, Billing, Receptionist
            // claims.manage → Admin, Billing
            migrationBuilder.Sql(@"
WITH new_perms (role_name, permission) AS (
    VALUES
        ('Admin',        'dispensary.view'),
        ('Admin',        'dispensary.manage'),
        ('Doctor',       'dispensary.view'),
        ('Nurse',        'dispensary.view'),
        ('Nurse',        'dispensary.manage'),
        ('Admin',        'claims.view'),
        ('Admin',        'claims.manage'),
        ('Doctor',       'claims.view'),
        ('Billing',      'claims.view'),
        ('Billing',      'claims.manage'),
        ('Receptionist', 'claims.view')
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
DELETE FROM identity.""RolePermissions""
WHERE ""PermissionKey"" IN (
    'dispensary.view', 'dispensary.manage',
    'claims.view', 'claims.manage'
);
");
        }
    }
}
