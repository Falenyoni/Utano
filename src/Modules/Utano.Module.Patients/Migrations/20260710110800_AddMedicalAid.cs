using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalAid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MedicalAidNumber",
                table: "Patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAidScheme",
                table: "Patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalAidNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicalAidScheme",
                table: "Patients");
        }
    }
}
