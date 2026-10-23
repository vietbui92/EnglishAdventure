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
        var user = await _fixture.CreateUserAsync(UserType.Admin);
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
            var storedUser = await db.Users.Include(candidate => candidate.Roles)
                .SingleAsync(candidate => candidate.Id == user.Id);
            var storedRole = await db.Roles.Include(candidate => candidate.Permissions)
                .SingleAsync(candidate => candidate.Id == role.Id);

            storedUser.AssignRole(storedRole.Id, DateTime.UtcNow);
            storedRole.GrantPermission(permission.Id);
            return await db.SaveChangesAsync();
        });

        var (roleIds, permissionIds) = await _fixture.WithDbContextAsync(async db =>
        (
            await db.Users.Where(candidate => candidate.Id == user.Id)
                .SelectMany(candidate => candidate.Roles).Select(assignment => assignment.RoleId).ToListAsync(),
            await db.Roles.Where(candidate => candidate.Id == role.Id)
                .SelectMany(candidate => candidate.Permissions).Select(grant => grant.PermissionId).ToListAsync()
        ));

        Assert.Equal([role.Id], roleIds);
        Assert.Equal([permission.Id], permissionIds);
    }

    [SqlServerFact]
    public async Task The_database_rejects_a_second_user_with_the_same_email_in_another_case()
    {
        var existing = await _fixture.CreateUserAsync(UserType.Parent);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _fixture.WithDbContextAsync(async db =>
        {
            db.Users.Add(User.Create(existing.Email.ToUpperInvariant(), "hash", UserType.Parent, DateTime.UtcNow));
            return await db.SaveChangesAsync();
        }));

        Assert.Contains("IX_auth_users_NormalizedEmail", exception.InnerException?.Message);
    }

    [SqlServerFact]
    public async Task Stored_password_hashes_verify_only_the_original_password()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent);

        var storedHash = await _fixture.WithDbContextAsync(db => db.Users
            .Where(candidate => candidate.Id == user.Id)
            .Select(candidate => candidate.PasswordHash)
            .SingleAsync());

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Assert.DoesNotContain(user.Password, storedHash);
        Assert.Equal(PasswordVerification.Succeeded, hasher.Verify(storedHash, user.Password));
        Assert.Equal(PasswordVerification.Failed, hasher.Verify(storedHash, user.Password + "x"));
    }

    // Permission codes allow only lower-case letters; the hex letters of a Guid keep each code unique per test run.
    private static string RandomLetters() =>
        new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray()) + "a";
}
