using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace EnglishAdventure.Api.IntegrationTests.Database;

public sealed class MigrateEndpointTests : IClassFixture<EnglishAdventureWebApplicationFactory>
{
    private readonly EnglishAdventureWebApplicationFactory _factory;

    public MigrateEndpointTests(EnglishAdventureWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Migrate_returns_not_found_outside_development_without_touching_the_database(string environment)
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));

        var response = await factory.CreateClient().PostAsync("/api/v1/database/migrate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
