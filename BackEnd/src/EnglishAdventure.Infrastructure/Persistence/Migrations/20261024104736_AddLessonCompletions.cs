using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_lesson_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BestStars = table.Column<int>(type: "int", nullable: false),
                    BestScorePercent = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BestAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_lesson_completions", x => x.Id);
                    table.CheckConstraint("CK_learning_lesson_completions_BestScorePercent", "[BestScorePercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_learning_lesson_completions_BestStars", "[BestStars] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_learning_lesson_completions_content_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "content_lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_learning_lesson_completions_family_child_profiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "family_child_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_lesson_completions_ChildProfileId_LessonId",
                table: "learning_lesson_completions",
                columns: new[] { "ChildProfileId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_lesson_completions_LessonId",
                table: "learning_lesson_completions",
                column: "LessonId");
            // Hand-written: lessons children already finished with a star (S4.1 attempts) count as completed, with their
            // best stars and score (stars only grow with the score).
            migrationBuilder.Sql(
                """
                INSERT INTO [learning_lesson_completions]
                    ([Id], [ChildProfileId], [LessonId], [BestStars], [BestScorePercent], [CompletedAtUtc], [BestAtUtc])
                SELECT NEWID(), [ChildProfileId], [LessonId], MAX([Stars]), MAX([ScorePercent]), MIN([FinishedAtUtc]), MAX([FinishedAtUtc])
                FROM [learning_lesson_attempts]
                WHERE [Status] = 2 AND [Stars] >= 1
                GROUP BY [ChildProfileId], [LessonId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_lesson_completions");
        }
    }
}
