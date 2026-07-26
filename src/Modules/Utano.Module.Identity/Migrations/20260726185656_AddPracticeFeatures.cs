using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeFeatures",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeFeatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeFeatures_PracticeId_FeatureKey",
                schema: "identity",
                table: "PracticeFeatures",
                columns: new[] { "PracticeId", "FeatureKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeFeatures",
                schema: "identity");
        }
    }
}
