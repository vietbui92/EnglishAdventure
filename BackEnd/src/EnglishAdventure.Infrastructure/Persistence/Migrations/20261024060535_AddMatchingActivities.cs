using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchingMode",
                table: "content_activities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "content_matching_pairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_matching_pairs", x => x.Id);
                    table.CheckConstraint("CK_content_matching_pairs_SortOrder", "[SortOrder] >= 1");
                    table.ForeignKey(
                        name: "FK_content_matching_pairs_content_activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "content_activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_matching_pairs_content_lesson_words_WordId",
                        column: x => x.WordId,
                        principalTable: "content_lesson_words",
                        principalColumn: "Id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_content_activities_MatchingMode",
                table: "content_activities",
                sql: "([Type] = 2 AND [MatchingMode] IN (1, 2)) OR ([Type] <> 2 AND [MatchingMode] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_content_matching_pairs_ActivityId_WordId",
                table: "content_matching_pairs",
                columns: new[] { "ActivityId", "WordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_matching_pairs_WordId",
                table: "content_matching_pairs",
                column: "WordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_matching_pairs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_content_activities_MatchingMode",
                table: "content_activities");

            migrationBuilder.DropColumn(
                name: "MatchingMode",
                table: "content_activities");
        }
    }
}
