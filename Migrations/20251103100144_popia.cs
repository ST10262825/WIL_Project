using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class popia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAcceptedPOPIA",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConsentUpdate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "POPIAAcceptedDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "POPIAVersion",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAcceptedPOPIA",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastConsentUpdate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "POPIAAcceptedDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "POPIAVersion",
                table: "Users");
        }
    }
}
