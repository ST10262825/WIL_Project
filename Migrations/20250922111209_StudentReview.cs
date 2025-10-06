using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class StudentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ReviewId",
                table: "Students",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Reviews_ReviewId",
                table: "Students",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Reviews_ReviewId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ReviewId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "Students");
        }
    }
}
