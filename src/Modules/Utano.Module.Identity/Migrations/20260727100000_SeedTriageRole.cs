using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260727100000_SeedTriageRole")]
    public partial class SeedTriageRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO identity.""Roles"" (""Id"", ""PracticeId"", ""Name"", ""Description"", ""IsSystem"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
SELECT gen_random_uuid(), p.""Id"", 'Triage', 'Patient triage and initial assessment', true, true, NOW(), NOW()
FROM identity.""Practices"" p
WHERE NOT EXISTS (
    SELECT 1 FROM identity.""Roles"" r
    WHERE r.""PracticeId"" = p.""Id"" AND r.""Name"" = 'Triage'
);
");

            migrationBuilder.Sql(@"
WITH triage_perms (permission) AS (
    VALUES
        ('patients.view'),
        ('patients.create'),
        ('appointments.view'),
        ('appointments.create'),
        ('clinical_notes.view'),
        ('clinical_notes.create')
)
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", tp.permission
FROM identity.""Roles"" r
JOIN triage_perms tp ON true
WHERE r.""Name"" = 'Triage'
AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = tp.permission
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions""
WHERE ""RoleId"" IN (
    SELECT ""Id"" FROM identity.""Roles"" WHERE ""Name"" = 'Triage'
);

DELETE FROM identity.""Roles"" WHERE ""Name"" = 'Triage';
");
        }
    }
}
