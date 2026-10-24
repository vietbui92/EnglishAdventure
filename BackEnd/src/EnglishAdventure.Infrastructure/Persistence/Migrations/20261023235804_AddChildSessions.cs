using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChildSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChildProfileId",
                table: "auth_refresh_tokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_refresh_tokens_ChildProfileId",
                table: "auth_refresh_tokens",
                column: "ChildProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_refresh_tokens_family_child_profiles_ChildProfileId",
                table: "auth_refresh_tokens",
                column: "ChildProfileId",
                principalTable: "family_child_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_auth_refresh_tokens_family_child_profiles_ChildProfileId",
                table: "auth_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_auth_refresh_tokens_ChildProfileId",
                table: "auth_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "ChildProfileId",
                table: "auth_refresh_tokens");
        }
    }
}
