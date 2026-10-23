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

    public async Task<TestAccount> CreateAccountAsync(
        AccountType accountType,
        bool isActive = true,
        params string[] roles)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        var account = Account.Create(email, passwordHasher.Hash(password), accountType, DateTime.UtcNow);

        if (!isActive)
        {
            account.Deactivate();
        }

        foreach (var roleName in roles)
        {
            var role = await dbContext.Roles.SingleOrDefaultAsync(candidate => candidate.Name == roleName);
            if (role is null)
            {
                role = Role.Create(roleName);
                dbContext.Roles.Add(role);
            }

            account.AssignRole(role.Id, DateTime.UtcNow);
        }

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return new TestAccount(account.Id, email, password);
    }

    public async Task<T> WithDbContextAsync<T>(Func<EnglishAdventureDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>());
    }
}

public sealed record TestAccount(Guid Id, string Email, string Password);
