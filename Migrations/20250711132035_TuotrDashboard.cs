using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class TuotrDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "Sessions");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TutorFeedback",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TutorFeedback",
                table: "Sessions");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
