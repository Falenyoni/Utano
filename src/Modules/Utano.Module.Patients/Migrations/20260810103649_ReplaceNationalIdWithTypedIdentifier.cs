using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Patients.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceNationalIdWithTypedIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_PracticeId_NationalId",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "IdentifierType",
                table: "Patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentifierValue",
                table: "Patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Backfill: every existing patient's NationalId becomes a NationalId-typed identifier.
            // Must happen before dropping the old column, and before IdentifierType is made
            // NOT NULL, since every existing row needs a value assigned first.
            migrationBuilder.Sql("""
                UPDATE "Patients" SET "IdentifierType" = 'NationalId', "IdentifierValue" = "NationalId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdentifierType",
                table: "Patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PracticeId_IdentifierType_IdentifierValue",
                table: "Patients",
                columns: new[] { "PracticeId", "IdentifierType", "IdentifierValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_PracticeId_IdentifierType_IdentifierValue",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Best-effort backfill for rollback: Passport/Pending patients have no National ID to
            // restore, so they get an empty string to satisfy the old NOT NULL constraint - this
            // is a one-way lossy path, only intended as a dev safety net, not a production rollback.
            migrationBuilder.Sql("""
                UPDATE "Patients" SET "NationalId" = COALESCE("IdentifierValue", '');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IdentifierType",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IdentifierValue",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PracticeId_NationalId",
                table: "Patients",
                columns: new[] { "PracticeId", "NationalId" },
                unique: true);
        }
    }
}
