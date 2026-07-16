using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoBase64",
                schema: "identity",
                table: "Practices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "identity",
                table: "Practices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoBase64",
                schema: "identity",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "identity",
                table: "Practices");
        }
    }
}
