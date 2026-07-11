using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.ClinicalNotes.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitProceduresAndDiagnoses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitDiagnoses",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    IcdCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    PracticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitDiagnoses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitProcedures",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NhrplCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PostedToInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    PracticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitProcedures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitDiagnoses_PracticeId",
                schema: "clinical",
                table: "VisitDiagnoses",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDiagnoses_VisitId",
                schema: "clinical",
                table: "VisitDiagnoses",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitProcedures_PracticeId",
                schema: "clinical",
                table: "VisitProcedures",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitProcedures_VisitId",
                schema: "clinical",
                table: "VisitProcedures",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitDiagnoses",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "VisitProcedures",
                schema: "clinical");
        }
    }
}
