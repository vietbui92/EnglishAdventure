using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.UnitTests.Exceptions;

/// <summary>
/// Example pattern for asserting domain rule violations once entities are introduced.
/// </summary>
public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_sets_message()
    {
        const string message = "A domain rule was violated.";

        var exception = new DomainException(message);

        Assert.Equal(message, exception.Message);
    }
}
