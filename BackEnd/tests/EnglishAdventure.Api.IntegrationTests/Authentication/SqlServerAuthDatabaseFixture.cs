using EnglishAdventure.Domain.Users;
using EnglishAdventure.Infrastructure.Identity;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnglishAdventure"] = connectionString
                })));

        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>().Database.MigrateAsync();
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            AccountType = accountType,
            IsActive = isActive
        };

        AssertSucceeded(await userManager.CreateAsync(user, password));

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                AssertSucceeded(await roleManager.CreateAsync(new ApplicationRole { Name = role }));
            }

            AssertSucceeded(await userManager.AddToRoleAsync(user, role));
        }

        return new TestAccount(user.Id, email, password);
    }

    public async Task<T> WithDbContextAsync<T>(Func<EnglishAdventureDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>());
    }

    private static void AssertSucceeded(IdentityResult result)
    {
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}

public sealed record TestAccount(Guid Id, string Email, string Password);
