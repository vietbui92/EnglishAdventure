using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// Persistence of the identity-schema aggregates on SQL Server: children added to an existing aggregate are inserted,
/// and the unique indexes back up the domain rules.
/// </summary>
public sealed class IdentityPersistenceSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private readonly SqlServerAuthDatabaseFixture _fixture;

    public IdentityPersistenceSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_role_and_a_permission_added_to_existing_aggregates_are_saved()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Admin);
        var role = Role.Create($"Role-{Guid.NewGuid():N}");
        var permission = Permission.Create("content.manage-" + RandomLetters());

        await _fixture.WithDbContextAsync(async db =>
        {
            db.Roles.Add(role);
            db.Permissions.Add(permission);
            return await db.SaveChangesAsync();
        });

        await _fixture.WithDbContextAsync(async db =>
        {
            var storedAccount = await db.Accounts.Include(candidate => candidate.Roles)
                .SingleAsync(candidate => candidate.Id == account.Id);
            var storedRole = await db.Roles.Include(candidate => candidate.Permissions)
                .SingleAsync(candidate => candidate.Id == role.Id);

            storedAccount.AssignRole(storedRole.Id, DateTime.UtcNow);
            storedRole.GrantPermission(permission.Id);
            return await db.SaveChangesAsync();
        });

        var (roleIds, permissionIds) = await _fixture.WithDbContextAsync(async db =>
        (
            await db.Accounts.Where(candidate => candidate.Id == account.Id)
                .SelectMany(candidate => candidate.Roles).Select(assignment => assignment.RoleId).ToListAsync(),
            await db.Roles.Where(candidate => candidate.Id == role.Id)
                .SelectMany(candidate => candidate.Permissions).Select(grant => grant.PermissionId).ToListAsync()
        ));

        Assert.Equal([role.Id], roleIds);
        Assert.Equal([permission.Id], permissionIds);
    }

    [SqlServerFact]
    public async Task The_database_rejects_a_second_account_with_the_same_email_in_another_case()
    {
        var existing = await _fixture.CreateAccountAsync(AccountType.Parent);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _fixture.WithDbContextAsync(async db =>
        {
            db.Accounts.Add(Account.Create(existing.Email.ToUpperInvariant(), "hash", AccountType.Parent, DateTime.UtcNow));
            return await db.SaveChangesAsync();
        }));

        Assert.Contains("IX_auth_accounts_NormalizedEmail", exception.InnerException?.Message);
    }

    [SqlServerFact]
    public async Task Stored_password_hashes_verify_only_the_original_password()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);

        var storedHash = await _fixture.WithDbContextAsync(db => db.Accounts
            .Where(candidate => candidate.Id == account.Id)
            .Select(candidate => candidate.PasswordHash)
            .SingleAsync());

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Assert.DoesNotContain(account.Password, storedHash);
        Assert.Equal(PasswordVerification.Succeeded, hasher.Verify(storedHash, account.Password));
        Assert.Equal(PasswordVerification.Failed, hasher.Verify(storedHash, account.Password + "x"));
    }

    // Permission codes allow only lower-case letters; the hex letters of a Guid keep each code unique per test run.
    private static string RandomLetters() =>
        new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray()) + "a";
}
