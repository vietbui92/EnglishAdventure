using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// Creates a uniquely named throwaway database on the SQL Server given by
/// <see cref="SqlServerFactAttribute.ConnectionStringVariable"/>, applies the real migrations to it, and drops
/// only that database afterwards. Does nothing when the variable is unset (the tests are then skipped).
/// </summary>
public sealed class SqlServerAuthDatabaseFixture : IAsyncLifetime
{
    private const string DatabaseNamePrefix = "EnglishAdventure_AuthTests_";

    private EnglishAdventureWebApplicationFactory? _rootFactory;
    private WebApplicationFactory<Program>? _factory;

    public WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("SQL Server integration tests are not configured.");

    public async Task InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return;
        }

        var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = DatabaseNamePrefix + Guid.NewGuid().ToString("N")
        }.ConnectionString;

        _rootFactory = new EnglishAdventureWebApplicationFactory();
        _factory = _rootFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(EnglishAdventureWebApplicationFactory.ConnectionStringKey, connectionString));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>().Database;

        // Guard: never run the tests against a database other than the throwaway one.
        var expectedName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (database.GetDbConnection().Database != expectedName)
        {
            throw new InvalidOperationException(
                $"Expected the throwaway database '{expectedName}' but the host uses '{database.GetDbConnection().Database}'.");
        }

        await database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is null || _rootFactory is null)
        {
            return;
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>().Database;

            // Guard: only ever drop the database this fixture created.
            if (database.GetDbConnection().Database.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal))
            {
                await database.EnsureDeletedAsync();
            }
        }

        await _rootFactory.DisposeAsync();
    }

    public async Task<TestUser> CreateUserAsync(
        UserType userType,
        bool isActive = true,
        params string[] roles)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        var user = User.Create(email, passwordHasher.Hash(password), userType, DateTime.UtcNow);

        if (!isActive)
        {
            user.Deactivate();
        }

        foreach (var roleName in roles)
        {
            var role = await dbContext.Roles.SingleOrDefaultAsync(candidate => candidate.Name == roleName);
            if (role is null)
            {
                role = Role.Create(roleName);
                dbContext.Roles.Add(role);
            }

            user.AssignRole(role.Id, DateTime.UtcNow);
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return new TestUser(user.Id, email, password);
    }

    /// <summary>
    /// Grants permissions to a role, creating the role and permission rows when they do not exist yet.
    /// </summary>
    public async Task GrantPermissionsAsync(string roleName, params string[] permissionCodes)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>();

        var role = await dbContext.Roles.Include(candidate => candidate.Permissions)
            .SingleOrDefaultAsync(candidate => candidate.Name == roleName);
        if (role is null)
        {
            role = Role.Create(roleName);
            dbContext.Roles.Add(role);
        }

        foreach (var code in permissionCodes)
        {
            var permission = await dbContext.Permissions.SingleOrDefaultAsync(candidate => candidate.Code == code);
            if (permission is null)
            {
                permission = Permission.Create(code);
                dbContext.Permissions.Add(permission);
            }

            role.GrantPermission(permission.Id);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<T> WithDbContextAsync<T>(Func<EnglishAdventureDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>());
    }
}

public sealed record TestUser(Guid Id, string Email, string Password);
