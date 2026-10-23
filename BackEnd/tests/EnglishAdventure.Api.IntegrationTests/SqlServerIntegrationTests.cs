using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Api.IntegrationTests;

/// <summary>
/// Optional SQL Server connectivity checks against a real instance (not EF InMemory).
/// </summary>
public sealed class SqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task Database_can_connect_when_ENGLISH_ADVENTURE_TEST_SQL_CONNECTION_is_set()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionStringVariable);

        var optionsBuilder = new DbContextOptionsBuilder<EnglishAdventureDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var testContext = new EnglishAdventureDbContext(optionsBuilder.Options);

        var canConnect = await testContext.Database.CanConnectAsync();
        Assert.True(canConnect, "Expected to connect to SQL Server using ENGLISH_ADVENTURE_TEST_SQL_CONNECTION.");
    }
}
