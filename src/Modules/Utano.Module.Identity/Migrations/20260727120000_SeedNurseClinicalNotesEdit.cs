using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260727120000_SeedNurseClinicalNotesEdit")]
    public partial class SeedNurseClinicalNotesEdit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", 'clinical_notes.edit'
FROM identity.""Roles"" r
WHERE r.""Name"" = 'Nurse'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = 'clinical_notes.edit'
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions"" rp
USING identity.""Roles"" r
WHERE rp.""RoleId"" = r.""Id""
  AND r.""Name"" = 'Nurse'
  AND rp.""PermissionKey"" = 'clinical_notes.edit';
");
        }
    }
}
