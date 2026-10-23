using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Renames "account" to "user" everywhere (owner request, 2026-10-23): tables, columns, keys, indexes and the
    /// users.* permission codes. Written by hand as renames: EF scaffolded a drop-and-create, which would have deleted
    /// every user. All rows, role assignments and sessions are kept.
    /// </summary>
    public partial class RenameAccountsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Constraints carry the old names; drop them first and recreate them under the new names at the end.
            migrationBuilder.DropForeignKey(name: "FK_auth_refresh_tokens_auth_accounts_AccountId", table: "auth_refresh_tokens");
            migrationBuilder.DropForeignKey(name: "FK_auth_account_roles_auth_accounts_AccountId", table: "auth_account_roles");
            migrationBuilder.DropForeignKey(name: "FK_auth_account_roles_auth_accounts_AssignedByAccountId", table: "auth_account_roles");
            migrationBuilder.DropForeignKey(name: "FK_auth_account_roles_auth_roles_RoleId", table: "auth_account_roles");
            migrationBuilder.DropCheckConstraint(name: "CK_auth_accounts_AccountType", table: "auth_accounts");

            migrationBuilder.RenameTable(name: "auth_accounts", newName: "auth_users");
            migrationBuilder.RenameTable(name: "auth_account_roles", newName: "auth_user_roles");

            migrationBuilder.Sql("EXEC sp_rename N'[PK_auth_accounts]', N'PK_auth_users', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[PK_auth_account_roles]', N'PK_auth_user_roles', N'OBJECT';");

            migrationBuilder.RenameColumn(name: "AccountType", table: "auth_users", newName: "UserType");
            migrationBuilder.RenameColumn(name: "AccountId", table: "auth_user_roles", newName: "UserId");
            migrationBuilder.RenameColumn(name: "AssignedByAccountId", table: "auth_user_roles", newName: "AssignedByUserId");
            migrationBuilder.RenameColumn(name: "AccountId", table: "auth_refresh_tokens", newName: "UserId");

            migrationBuilder.RenameIndex(name: "IX_auth_accounts_NormalizedEmail", table: "auth_users", newName: "IX_auth_users_NormalizedEmail");
            migrationBuilder.RenameIndex(name: "IX_auth_account_roles_AccountId_RoleId", table: "auth_user_roles", newName: "IX_auth_user_roles_UserId_RoleId");
            migrationBuilder.RenameIndex(name: "IX_auth_account_roles_AssignedByAccountId", table: "auth_user_roles", newName: "IX_auth_user_roles_AssignedByUserId");
            migrationBuilder.RenameIndex(name: "IX_auth_account_roles_RoleId", table: "auth_user_roles", newName: "IX_auth_user_roles_RoleId");
            migrationBuilder.RenameIndex(name: "IX_auth_refresh_tokens_AccountId", table: "auth_refresh_tokens", newName: "IX_auth_refresh_tokens_UserId");

            migrationBuilder.AddCheckConstraint(name: "CK_auth_users_UserType", table: "auth_users", sql: "[UserType] IN (1, 2, 3)");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_user_roles_auth_users_UserId",
                table: "auth_user_roles",
                column: "UserId",
                principalTable: "auth_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_user_roles_auth_users_AssignedByUserId",
                table: "auth_user_roles",
                column: "AssignedByUserId",
                principalTable: "auth_users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_user_roles_auth_roles_RoleId",
                table: "auth_user_roles",
                column: "RoleId",
                principalTable: "auth_roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_refresh_tokens_auth_users_UserId",
                table: "auth_refresh_tokens",
                column: "UserId",
                principalTable: "auth_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // The permission codes follow the rename; roles keep their existing grants.
            migrationBuilder.Sql("UPDATE [auth_permissions] SET [Code] = N'users.read' WHERE [Code] = N'accounts.read';");
            migrationBuilder.Sql("UPDATE [auth_permissions] SET [Code] = N'users.manage' WHERE [Code] = N'accounts.manage';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [auth_permissions] SET [Code] = N'accounts.read' WHERE [Code] = N'users.read';");
            migrationBuilder.Sql("UPDATE [auth_permissions] SET [Code] = N'accounts.manage' WHERE [Code] = N'users.manage';");

            migrationBuilder.DropForeignKey(name: "FK_auth_refresh_tokens_auth_users_UserId", table: "auth_refresh_tokens");
            migrationBuilder.DropForeignKey(name: "FK_auth_user_roles_auth_users_UserId", table: "auth_user_roles");
            migrationBuilder.DropForeignKey(name: "FK_auth_user_roles_auth_users_AssignedByUserId", table: "auth_user_roles");
            migrationBuilder.DropForeignKey(name: "FK_auth_user_roles_auth_roles_RoleId", table: "auth_user_roles");
            migrationBuilder.DropCheckConstraint(name: "CK_auth_users_UserType", table: "auth_users");

            migrationBuilder.RenameIndex(name: "IX_auth_refresh_tokens_UserId", table: "auth_refresh_tokens", newName: "IX_auth_refresh_tokens_AccountId");
            migrationBuilder.RenameIndex(name: "IX_auth_user_roles_RoleId", table: "auth_user_roles", newName: "IX_auth_account_roles_RoleId");
            migrationBuilder.RenameIndex(name: "IX_auth_user_roles_AssignedByUserId", table: "auth_user_roles", newName: "IX_auth_account_roles_AssignedByAccountId");
            migrationBuilder.RenameIndex(name: "IX_auth_user_roles_UserId_RoleId", table: "auth_user_roles", newName: "IX_auth_account_roles_AccountId_RoleId");
            migrationBuilder.RenameIndex(name: "IX_auth_users_NormalizedEmail", table: "auth_users", newName: "IX_auth_accounts_NormalizedEmail");

            migrationBuilder.RenameColumn(name: "UserId", table: "auth_refresh_tokens", newName: "AccountId");
            migrationBuilder.RenameColumn(name: "AssignedByUserId", table: "auth_user_roles", newName: "AssignedByAccountId");
            migrationBuilder.RenameColumn(name: "UserId", table: "auth_user_roles", newName: "AccountId");
            migrationBuilder.RenameColumn(name: "UserType", table: "auth_users", newName: "AccountType");

            migrationBuilder.Sql("EXEC sp_rename N'[PK_auth_user_roles]', N'PK_auth_account_roles', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[PK_auth_users]', N'PK_auth_accounts', N'OBJECT';");

            migrationBuilder.RenameTable(name: "auth_user_roles", newName: "auth_account_roles");
            migrationBuilder.RenameTable(name: "auth_users", newName: "auth_accounts");

            migrationBuilder.AddCheckConstraint(name: "CK_auth_accounts_AccountType", table: "auth_accounts", sql: "[AccountType] IN (1, 2, 3)");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_account_roles_auth_accounts_AccountId",
                table: "auth_account_roles",
                column: "AccountId",
                principalTable: "auth_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_account_roles_auth_accounts_AssignedByAccountId",
                table: "auth_account_roles",
                column: "AssignedByAccountId",
                principalTable: "auth_accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_account_roles_auth_roles_RoleId",
                table: "auth_account_roles",
                column: "RoleId",
                principalTable: "auth_roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_refresh_tokens_auth_accounts_AccountId",
                table: "auth_refresh_tokens",
                column: "AccountId",
                principalTable: "auth_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
