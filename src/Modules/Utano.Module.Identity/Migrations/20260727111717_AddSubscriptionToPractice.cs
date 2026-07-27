using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionToPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubscriptionExpiresAt",
                schema: "identity",
                table: "Practices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                schema: "identity",
                table: "Practices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Trial");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionTier",
                schema: "identity",
                table: "Practices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Starter");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrialEndsAt",
                schema: "identity",
                table: "Practices",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAt",
                schema: "identity",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                schema: "identity",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                schema: "identity",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                schema: "identity",
                table: "Practices");
        }
    }
}
