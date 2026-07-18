using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Files.Migrations
{
    /// <inheritdoc />
    public partial class InitFilesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PracticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_ConsultationId",
                table: "FileAttachments",
                column: "ConsultationId",
                filter: "\"ConsultationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_PracticeId",
                table: "FileAttachments",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_PracticeId_PatientId",
                table: "FileAttachments",
                columns: new[] { "PracticeId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_PracticeId_PatientId_AttachmentType",
                table: "FileAttachments",
                columns: new[] { "PracticeId", "PatientId", "AttachmentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileAttachments");
        }
    }
}
