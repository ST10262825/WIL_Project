using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class Booking_rating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReviewId",
                table: "Bookings",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Reviews_ReviewId",
                table: "Bookings",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Reviews_ReviewId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReviewId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "Bookings");
        }
    }
}
