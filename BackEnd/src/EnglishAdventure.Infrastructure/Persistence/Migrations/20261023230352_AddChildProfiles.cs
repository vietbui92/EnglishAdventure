using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChildProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_child_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    AvatarKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_child_profiles", x => x.Id);
                    table.CheckConstraint("CK_family_child_profiles_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_family_child_profiles_auth_users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_child_profiles_ParentUserId",
                table: "family_child_profiles",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_family_child_profiles_ParentUserId_Nickname",
                table: "family_child_profiles",
                columns: new[] { "ParentUserId", "Nickname" },
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_child_profiles");
        }
    }
}
