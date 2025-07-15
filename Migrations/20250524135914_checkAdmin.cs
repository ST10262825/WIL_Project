using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class checkAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicUrl",
                table: "Tutors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicUrl",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
