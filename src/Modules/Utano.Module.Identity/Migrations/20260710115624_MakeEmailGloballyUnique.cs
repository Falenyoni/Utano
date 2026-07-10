using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailGloballyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
