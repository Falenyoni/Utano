using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddAdhozAndBpNumberToPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdhozNumber",
                schema: "identity",
                table: "Practices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BpNumber",
                schema: "identity",
                table: "Practices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdhozNumber",
                schema: "identity",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "BpNumber",
                schema: "identity",
                table: "Practices");
        }
    }
}
