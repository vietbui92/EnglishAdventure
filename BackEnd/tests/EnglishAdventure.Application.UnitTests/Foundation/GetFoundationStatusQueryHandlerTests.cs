using EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;

namespace EnglishAdventure.Application.UnitTests.Foundation;

public sealed class GetFoundationStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_foundation_example_response()
    {
        var handler = new GetFoundationStatusQueryHandler();

        var response = await handler.Handle(
            new GetFoundationStatusQuery("ping"),
            CancellationToken.None);

        Assert.True(response.IsFoundationExample);
        Assert.Equal("ping", response.Echo);
        Assert.Contains("foundation", response.Message, StringComparison.OrdinalIgnoreCase);
    }
}
