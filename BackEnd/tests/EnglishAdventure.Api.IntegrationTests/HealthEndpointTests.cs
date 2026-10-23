using System.Net;

namespace EnglishAdventure.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<EnglishAdventureWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(EnglishAdventureWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_success_without_using_the_database()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
