using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentLevelsAndLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_levels", x => x.Id);
                    table.CheckConstraint("CK_content_levels_SortOrder", "[SortOrder] >= 1");
                    table.CheckConstraint("CK_content_levels_Status", "[Status] IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "content_lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_lessons", x => x.Id);
                    table.CheckConstraint("CK_content_lessons_SortOrder", "[SortOrder] >= 1");
                    table.CheckConstraint("CK_content_lessons_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_content_lessons_content_levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "content_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_lessons_LevelId",
                table: "content_lessons",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_content_lessons_LevelId_SortOrder",
                table: "content_lessons",
                columns: new[] { "LevelId", "SortOrder" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_content_levels_SortOrder",
                table: "content_levels",
                column: "SortOrder",
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_lessons");

            migrationBuilder.DropTable(
                name: "content_levels");
        }
    }
}
