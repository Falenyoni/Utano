using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Key);
                });

            // Seed the catalog with every permission key currently declared across all
            // IModuleDescriptor implementations, before the FK below is added - guarantees the FK
            // validates cleanly regardless of whether this migration or
            // SeedGranularPracticePermissions (20260803090000) happens to run first, since either
            // order leaves every referenced key present in this table by the time the FK is
            // checked. PermissionReconciler keeps this catalog in sync with code going forward.
            migrationBuilder.Sql(@"
INSERT INTO identity.""Permissions"" (""Key"") VALUES
    ('settings.users.view'),
    ('settings.users.manage'),
    ('settings.roles'),
    ('settings.staff.view'),
    ('settings.staff.manage'),
    ('settings.practice'),
    ('settings.branding'),
    ('settings.medical_aids'),
    ('settings.subscription'),
    ('notifications.view'),
    ('reports.view'),
    ('clinical_notes.view'),
    ('clinical_notes.create'),
    ('clinical_notes.edit'),
    ('triage.create'),
    ('dispensary.view'),
    ('dispensary.manage'),
    ('billing.view'),
    ('billing.manage'),
    ('claims.view'),
    ('claims.manage'),
    ('settings.billing_config.view'),
    ('settings.billing_config.manage'),
    ('appointments.view'),
    ('appointments.create'),
    ('appointments.edit'),
    ('appointments.cancel'),
    ('patients.view'),
    ('patients.create'),
    ('patients.edit'),
    ('patients.delete'),
    ('patients.refer'),
    ('inventory.view'),
    ('inventory.manage');
");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionKey",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionKey");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionKey",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionKey",
                principalSchema: "identity",
                principalTable: "Permissions",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionKey",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionKey",
                schema: "identity",
                table: "RolePermissions");
        }
    }
}
