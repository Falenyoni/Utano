using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Patients.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPatientAndAddMedicalAids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalAidScheme",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "Patients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "Patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChronicConditions",
                table: "Patients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalAidId",
                table: "Patients",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalAids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalAids", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAids_PracticeId_Code",
                table: "MedicalAids",
                columns: new[] { "PracticeId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalAids");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ChronicConditions",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicalAidId",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "MedicalAidScheme",
                table: "Patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
