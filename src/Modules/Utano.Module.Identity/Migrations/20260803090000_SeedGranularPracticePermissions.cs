using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260803090000_SeedGranularPracticePermissions")]
    public partial class SeedGranularPracticePermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "settings.practice" used to gate Practice details, Branding, Medical Aid Schemes,
            // and Subscription all at once - splitting it into 4 distinct permissions so they can
            // be granted independently (Subscription especially shouldn't be bundled with the
            // others). Purely additive: any role that already holds settings.practice gets the 3
            // new keys too, so nobody's effective access changes on upgrade. settings.practice
            // itself is untouched and keeps gating Practice details specifically.
            migrationBuilder.Sql(@"
WITH new_perms (permission) AS (
    VALUES
        ('settings.branding'),
        ('settings.medical_aids'),
        ('settings.subscription')
)
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT rp.""RoleId"", np.permission
FROM identity.""RolePermissions"" rp
JOIN new_perms np ON true
WHERE rp.""PermissionKey"" = 'settings.practice'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp2
    WHERE rp2.""RoleId"" = rp.""RoleId"" AND rp2.""PermissionKey"" = np.permission
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions""
WHERE ""PermissionKey"" IN ('settings.branding', 'settings.medical_aids', 'settings.subscription');
");
        }
    }
}
