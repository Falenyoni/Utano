using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260722120000_RevertToPerPracticeEmailAndAddReferPermission")]
    public partial class RevertToPerPracticeEmailAndAddReferPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                schema: "identity",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PracticeId_Email",
                schema: "identity",
                table: "Users",
                columns: new[] { "PracticeId", "Email" },
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"")
SELECT r.""Id"", 'patients.refer'
FROM identity.""Roles"" r
WHERE r.""Name"" IN ('Admin', 'Doctor', 'Nurse')
  AND NOT EXISTS (
    SELECT 1 FROM identity.""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionKey"" = 'patients.refer'
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""RolePermissions""
WHERE ""PermissionKey"" = 'patients.refer';
");

            migrationBuilder.DropIndex(
                name: "IX_Users_PracticeId_Email",
                schema: "identity",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "identity",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
