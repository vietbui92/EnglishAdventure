using System.Net;
using System.Net.Http.Json;
using EnglishAdventure.Application.Database.Commands.ApplyMigrations;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Database;

/// <summary>
/// Calls the migrate endpoint against a uniquely named database that does not exist yet, then drops only that
/// database.
/// </summary>
public sealed class MigrateEndpointSqlServerTests
{
    private const string MigrateRoute = "/api/v1/database/migrate";
    private const string DatabaseNamePrefix = "EnglishAdventure_MigrationTests_";

    [SqlServerFact]
    public async Task Migrate_creates_the_database_and_applies_migrations_once()
    {
        var databaseName = DatabaseNamePrefix + Guid.NewGuid().ToString("N");
        var connectionString = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionStringVariable))
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        await using var rootFactory = new EnglishAdventureWebApplicationFactory();
        await using var factory = rootFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(EnglishAdventureWebApplicationFactory.ConnectionStringKey, connectionString));

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>().Database;
                Assert.Equal(databaseName, database.GetDbConnection().Database);
                Assert.False(await database.CanConnectAsync(), "The throwaway database should not exist yet.");
            }

            var client = factory.CreateClient();

            var first = await client.PostAsync(MigrateRoute, content: null);
            var second = await client.PostAsync(MigrateRoute, content: null);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var firstBody = await first.Content.ReadFromJsonAsync<ApplyMigrationsResponse>();
            Assert.Contains(firstBody!.AppliedMigrations, name => name.EndsWith("_InitialCreate", StringComparison.Ordinal));

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var secondBody = await second.Content.ReadFromJsonAsync<ApplyMigrationsResponse>();
            Assert.Empty(secondBody!.AppliedMigrations);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>().Database;

            // Guard: only ever drop the database this test created.
            if (database.GetDbConnection().Database.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal))
            {
                await database.EnsureDeletedAsync();
            }
        }
    }
}
