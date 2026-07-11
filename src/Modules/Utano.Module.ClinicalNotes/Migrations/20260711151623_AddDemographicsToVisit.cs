using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.ClinicalNotes.Migrations
{
    /// <inheritdoc />
    public partial class AddDemographicsToVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PatientDateOfBirth",
                schema: "clinical",
                table: "Visits",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientGender",
                schema: "clinical",
                table: "Visits",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientDateOfBirth",
                schema: "clinical",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "PatientGender",
                schema: "clinical",
                table: "Visits");
        }
    }
}
