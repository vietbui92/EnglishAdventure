using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAcceptedAtUtc",
                table: "auth_users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "auth_users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAtUtc",
                table: "auth_users",
                type: "datetime2",
                nullable: true);

            // Hand-written: every existing user was created by an administrator or the bootstrap command, so their
            // email counts as confirmed. Without this they could no longer sign in. EXEC compiles the statement only
            // when it runs; in a script, SQL Server would otherwise reject the column added in the same batch.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE [auth_users] SET [EmailConfirmedAtUtc] = [CreatedAtUtc] WHERE [EmailConfirmedAtUtc] IS NULL;');");

            migrationBuilder.CreateTable(
                name: "auth_email_confirmation_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_email_confirmation_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auth_email_confirmation_tokens_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_email_confirmation_tokens_ExpiresAtUtc",
                table: "auth_email_confirmation_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_auth_email_confirmation_tokens_TokenHash",
                table: "auth_email_confirmation_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_email_confirmation_tokens_UserId",
                table: "auth_email_confirmation_tokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_email_confirmation_tokens");

            migrationBuilder.DropColumn(
                name: "ConsentAcceptedAtUtc",
                table: "auth_users");

            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "auth_users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAtUtc",
                table: "auth_users");
        }
    }
}
