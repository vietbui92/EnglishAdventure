using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_lesson_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    CorrectItems = table.Column<int>(type: "int", nullable: true),
                    ScorePercent = table.Column<int>(type: "int", nullable: true),
                    Stars = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_lesson_attempts", x => x.Id);
                    table.CheckConstraint("CK_learning_lesson_attempts_Stars", "[Stars] IS NULL OR [Stars] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_learning_lesson_attempts_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_learning_lesson_attempts_content_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "content_lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_learning_lesson_attempts_family_child_profiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "family_child_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "learning_attempt_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChosenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    TryNumber = table.Column<int>(type: "int", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_attempt_answers", x => x.Id);
                    table.CheckConstraint("CK_learning_attempt_answers_TryNumber", "[TryNumber] >= 1");
                    table.ForeignKey(
                        name: "FK_learning_attempt_answers_learning_lesson_attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "learning_lesson_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_attempt_answers_AttemptId_ItemId_TryNumber",
                table: "learning_attempt_answers",
                columns: new[] { "AttemptId", "ItemId", "TryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_lesson_attempts_ChildProfileId_LessonId",
                table: "learning_lesson_attempts",
                columns: new[] { "ChildProfileId", "LessonId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_learning_lesson_attempts_LessonId",
                table: "learning_lesson_attempts",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_attempt_answers");

            migrationBuilder.DropTable(
                name: "learning_lesson_attempts");
        }
    }
}
