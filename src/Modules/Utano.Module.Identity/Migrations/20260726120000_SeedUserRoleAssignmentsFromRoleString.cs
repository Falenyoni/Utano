using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260726120000_SeedUserRoleAssignmentsFromRoleString")]
    public partial class SeedUserRoleAssignmentsFromRoleString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // For every user that has no UserRoles entry, insert one by matching
            // User.Role (the denormalized string) to Roles.Name within the same practice.
            migrationBuilder.Sql(@"
INSERT INTO identity.""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
SELECT u.""Id"", r.""Id"", NOW()
FROM identity.""Users"" u
JOIN identity.""Roles"" r
  ON r.""Name"" = u.""Role""
 AND r.""PracticeId"" = u.""PracticeId""
WHERE NOT EXISTS (
    SELECT 1 FROM identity.""UserRoles"" ur
    WHERE ur.""UserId"" = u.""Id""
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Non-destructive rollback: only removes rows we can be certain were seeded
            // (users whose sole role assignment matches their Role string).
            migrationBuilder.Sql(@"
DELETE FROM identity.""UserRoles""
WHERE (""UserId"", ""RoleId"") IN (
    SELECT u.""Id"", r.""Id""
    FROM identity.""Users"" u
    JOIN identity.""Roles"" r
      ON r.""Name"" = u.""Role""
     AND r.""PracticeId"" = u.""PracticeId""
);
");
        }
    }
}
