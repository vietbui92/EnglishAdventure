using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_activities", x => x.Id);
                    table.CheckConstraint("CK_content_activities_SortOrder", "[SortOrder] >= 1");
                    table.CheckConstraint("CK_content_activities_Type", "[Type] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_content_activities_content_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "content_lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_quiz_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Form = table.Column<int>(type: "int", nullable: false),
                    CorrectWordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sentence = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_quiz_questions", x => x.Id);
                    table.CheckConstraint("CK_content_quiz_questions_Form", "[Form] IN (1, 2, 3)");
                    table.CheckConstraint("CK_content_quiz_questions_SortOrder", "[SortOrder] >= 1");
                    table.ForeignKey(
                        name: "FK_content_quiz_questions_content_activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "content_activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_quiz_questions_content_lesson_words_CorrectWordId",
                        column: x => x.CorrectWordId,
                        principalTable: "content_lesson_words",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "content_quiz_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_quiz_options", x => x.Id);
                    table.CheckConstraint("CK_content_quiz_options_SortOrder", "[SortOrder] >= 1");
                    table.ForeignKey(
                        name: "FK_content_quiz_options_content_lesson_words_WordId",
                        column: x => x.WordId,
                        principalTable: "content_lesson_words",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_quiz_options_content_quiz_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "content_quiz_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_activities_LessonId",
                table: "content_activities",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_content_quiz_options_QuestionId_WordId",
                table: "content_quiz_options",
                columns: new[] { "QuestionId", "WordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_quiz_options_WordId",
                table: "content_quiz_options",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_content_quiz_questions_ActivityId",
                table: "content_quiz_questions",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_content_quiz_questions_CorrectWordId",
                table: "content_quiz_questions",
                column: "CorrectWordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_quiz_options");

            migrationBuilder.DropTable(
                name: "content_quiz_questions");

            migrationBuilder.DropTable(
                name: "content_activities");
        }
    }
}
