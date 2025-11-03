using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class DeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterialFolders_Tutors_TutorId1",
                table: "LearningMaterialFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId1",
                table: "LearningMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Students_StudentId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaceItems_VirtualLearningSpaces_SpaceId",
                table: "SpaceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId1",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_GamificationProfiles_GamificationProfileId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_VirtualLearningSpaces_Users_UserId",
                table: "VirtualLearningSpaces");

            migrationBuilder.DropIndex(
                name: "IX_StudentMaterialAccesses_StudentId1",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropIndex(
                name: "IX_LearningMaterials_TutorId1",
                table: "LearningMaterials");

            migrationBuilder.DropIndex(
                name: "IX_LearningMaterialFolders_TutorId1",
                table: "LearningMaterialFolders");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropColumn(
                name: "TutorId1",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "TutorId1",
                table: "LearningMaterialFolders");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Students_StudentId",
                table: "Reviews",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaceItems_VirtualLearningSpaces_SpaceId",
                table: "SpaceItems",
                column: "SpaceId",
                principalTable: "VirtualLearningSpaces",
                principalColumn: "SpaceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId",
                table: "StudentMaterialAccesses",
                column: "LearningMaterialId",
                principalTable: "LearningMaterials",
                principalColumn: "LearningMaterialId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId",
                table: "StudentMaterialAccesses",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_GamificationProfiles_GamificationProfileId",
                table: "UserAchievements",
                column: "GamificationProfileId",
                principalTable: "GamificationProfiles",
                principalColumn: "GamificationProfileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VirtualLearningSpaces_Users_UserId",
                table: "VirtualLearningSpaces",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Students_StudentId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaceItems_VirtualLearningSpaces_SpaceId",
                table: "SpaceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId",
                table: "StudentMaterialAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_GamificationProfiles_GamificationProfileId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_VirtualLearningSpaces_Users_UserId",
                table: "VirtualLearningSpaces");

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "StudentMaterialAccesses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TutorId1",
                table: "LearningMaterials",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TutorId1",
                table: "LearningMaterialFolders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentMaterialAccesses_StudentId1",
                table: "StudentMaterialAccesses",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_TutorId1",
                table: "LearningMaterials",
                column: "TutorId1");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterialFolders_TutorId1",
                table: "LearningMaterialFolders",
                column: "TutorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterialFolders_Tutors_TutorId1",
                table: "LearningMaterialFolders",
                column: "TutorId1",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId",
                table: "LearningMaterials",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_Tutors_TutorId1",
                table: "LearningMaterials",
                column: "TutorId1",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Students_StudentId",
                table: "Reviews",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SpaceItems_VirtualLearningSpaces_SpaceId",
                table: "SpaceItems",
                column: "SpaceId",
                principalTable: "VirtualLearningSpaces",
                principalColumn: "SpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMaterialAccesses_LearningMaterials_LearningMaterialId",
                table: "StudentMaterialAccesses",
                column: "LearningMaterialId",
                principalTable: "LearningMaterials",
                principalColumn: "LearningMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId",
                table: "StudentMaterialAccesses",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMaterialAccesses_Students_StudentId1",
                table: "StudentMaterialAccesses",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_GamificationProfiles_GamificationProfileId",
                table: "UserAchievements",
                column: "GamificationProfileId",
                principalTable: "GamificationProfiles",
                principalColumn: "GamificationProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_VirtualLearningSpaces_Users_UserId",
                table: "VirtualLearningSpaces",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
