using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBossChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_content_lessons_LevelId_SortOrder",
                table: "content_lessons");

            migrationBuilder.AddColumn<string>(
                name: "BossImageUrl",
                table: "content_lessons",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Hand-edited: lessons that already exist are regular lessons (LessonKind.Regular = 1); 0 would break the
            // Kind check constraint below. The model has no default value, so the app always writes the column itself.
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "content_lessons",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_content_lessons_LevelId_Boss",
                table: "content_lessons",
                column: "LevelId",
                unique: true,
                filter: "[Status] <> 3 AND [Kind] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_content_lessons_LevelId_SortOrder",
                table: "content_lessons",
                columns: new[] { "LevelId", "SortOrder" },
                unique: true,
                filter: "[Status] <> 3 AND [Kind] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_lessons_BossImageUrl",
                table: "content_lessons",
                sql: "[Kind] = 2 OR [BossImageUrl] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_lessons_Kind",
                table: "content_lessons",
                sql: "[Kind] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_content_lessons_LevelId_Boss",
                table: "content_lessons");

            migrationBuilder.DropIndex(
                name: "IX_content_lessons_LevelId_SortOrder",
                table: "content_lessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_lessons_BossImageUrl",
                table: "content_lessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_lessons_Kind",
                table: "content_lessons");

            migrationBuilder.DropColumn(
                name: "BossImageUrl",
                table: "content_lessons");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "content_lessons");

            migrationBuilder.CreateIndex(
                name: "IX_content_lessons_LevelId_SortOrder",
                table: "content_lessons",
                columns: new[] { "LevelId", "SortOrder" },
                unique: true,
                filter: "[Status] <> 3");
        }
    }
}
