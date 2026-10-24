using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rewards_badge_awards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwardedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rewards_badge_awards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rewards_badge_awards_content_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "content_lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rewards_badge_awards_content_levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "content_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rewards_badge_awards_family_child_profiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "family_child_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rewards_badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rewards_badges", x => x.Id);
                    table.CheckConstraint("CK_rewards_badges_Kind", "[Kind] IN (1, 2, 3, 4)");
                });

            migrationBuilder.CreateTable(
                name: "rewards_points_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StarNumber = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AwardedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rewards_points_transactions", x => x.Id);
                    table.CheckConstraint("CK_rewards_points_transactions_Points", "[Points] > 0");
                    table.CheckConstraint("CK_rewards_points_transactions_StarNumber", "[StarNumber] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_rewards_points_transactions_content_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "content_lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rewards_points_transactions_family_child_profiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "family_child_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rewards_badge_awards_ChildProfileId_Kind_LevelId",
                table: "rewards_badge_awards",
                columns: new[] { "ChildProfileId", "Kind", "LevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rewards_badge_awards_LessonId",
                table: "rewards_badge_awards",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_rewards_badge_awards_LevelId",
                table: "rewards_badge_awards",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_rewards_badges_Kind",
                table: "rewards_badges",
                column: "Kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rewards_points_transactions_ChildProfileId_LessonId_StarNumber",
                table: "rewards_points_transactions",
                columns: new[] { "ChildProfileId", "LessonId", "StarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rewards_points_transactions_LessonId",
                table: "rewards_points_transactions",
                column: "LessonId");
            // Hand-written: the four badges of PD-011 with their default texts (the Admin can change them, S5.2).
            migrationBuilder.Sql(
                """
                INSERT INTO [rewards_badges] ([Id], [Kind], [Name], [Description], [ImageUrl]) VALUES
                    (NEWID(), 1, N'Bài học đầu tiên', N'Hoàn thành bài học đầu tiên.', NULL),
                    (NEWID(), 2, N'Hoàn hảo', N'Đạt 3 sao ở một bài học.', NULL),
                    (NEWID(), 3, N'Chinh phục {level}', N'Đánh bại boss của {level}.', NULL),
                    (NEWID(), 4, N'Siêu sao {level}', N'Đạt 3 sao ở mọi bài và boss của {level}.', NULL);
                """);

            // Hand-written: results earned before rewards existed are paid too (owner 2026-10-24): 10 points for each
            // star of each completion, and every badge whose condition is already met.
            migrationBuilder.Sql(
                """
                INSERT INTO [rewards_points_transactions] ([Id], [ChildProfileId], [LessonId], [StarNumber], [Points], [AwardedAtUtc])
                SELECT NEWID(), c.[ChildProfileId], c.[LessonId], s.[Number], 10, c.[BestAtUtc]
                FROM [learning_lesson_completions] c
                JOIN (VALUES (1), (2), (3)) s([Number]) ON s.[Number] <= c.[BestStars];

                -- First lesson: the earliest completion.
                INSERT INTO [rewards_badge_awards] ([Id], [ChildProfileId], [Kind], [LevelId], [LessonId], [AwardedAtUtc])
                SELECT NEWID(), f.[ChildProfileId], 1, NULL, f.[LessonId], f.[CompletedAtUtc]
                FROM (SELECT c.*, ROW_NUMBER() OVER (PARTITION BY c.[ChildProfileId] ORDER BY c.[CompletedAtUtc]) AS [Position]
                      FROM [learning_lesson_completions] c) f
                WHERE f.[Position] = 1;

                -- Perfect: the earliest three-star result.
                INSERT INTO [rewards_badge_awards] ([Id], [ChildProfileId], [Kind], [LevelId], [LessonId], [AwardedAtUtc])
                SELECT NEWID(), p.[ChildProfileId], 2, NULL, p.[LessonId], p.[BestAtUtc]
                FROM (SELECT c.*, ROW_NUMBER() OVER (PARTITION BY c.[ChildProfileId] ORDER BY c.[BestAtUtc]) AS [Position]
                      FROM [learning_lesson_completions] c
                      WHERE c.[BestStars] = 3) p
                WHERE p.[Position] = 1;

                -- Level conqueror: each defeated boss.
                INSERT INTO [rewards_badge_awards] ([Id], [ChildProfileId], [Kind], [LevelId], [LessonId], [AwardedAtUtc])
                SELECT NEWID(), c.[ChildProfileId], 3, l.[LevelId], c.[LessonId], c.[CompletedAtUtc]
                FROM [learning_lesson_completions] c
                JOIN [content_lessons] l ON l.[Id] = c.[LessonId]
                WHERE l.[Kind] = 2;

                -- Level superstar: three stars on every published lesson of a level that has a published boss.
                INSERT INTO [rewards_badge_awards] ([Id], [ChildProfileId], [Kind], [LevelId], [LessonId], [AwardedAtUtc])
                SELECT NEWID(), c.[ChildProfileId], 4, l.[LevelId],
                       (SELECT TOP 1 b.[Id] FROM [content_lessons] b WHERE b.[LevelId] = l.[LevelId] AND b.[Kind] = 2 AND b.[Status] = 2),
                       MAX(c.[BestAtUtc])
                FROM [learning_lesson_completions] c
                JOIN [content_lessons] l ON l.[Id] = c.[LessonId] AND l.[Status] = 2
                WHERE c.[BestStars] = 3
                GROUP BY c.[ChildProfileId], l.[LevelId]
                HAVING COUNT(*) = (SELECT COUNT(*) FROM [content_lessons] x WHERE x.[LevelId] = l.[LevelId] AND x.[Status] = 2)
                   AND EXISTS (SELECT 1 FROM [content_lessons] b WHERE b.[LevelId] = l.[LevelId] AND b.[Kind] = 2 AND b.[Status] = 2);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rewards_badge_awards");

            migrationBuilder.DropTable(
                name: "rewards_badges");

            migrationBuilder.DropTable(
                name: "rewards_points_transactions");
        }
    }
}
