using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.ClinicalNotes.Migrations
{
    /// <inheritdoc />
    public partial class LinkAppointmentToVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                schema: "clinical",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AppointmentId",
                schema: "clinical",
                table: "Visits",
                column: "AppointmentId",
                unique: true,
                filter: "\"AppointmentId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_AppointmentId",
                schema: "clinical",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                schema: "clinical",
                table: "Visits");
        }
    }
}
