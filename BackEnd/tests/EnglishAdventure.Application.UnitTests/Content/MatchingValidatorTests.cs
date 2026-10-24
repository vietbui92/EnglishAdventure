using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Commands.AddMatchingActivity;
using EnglishAdventure.Application.Content.Commands.UpdateMatchingActivity;

namespace EnglishAdventure.Application.UnitTests.Content;

public sealed class MatchingValidatorTests
{
    private static readonly string Version = ContentVersion.Encode([0, 0, 0, 0, 0, 0, 7, 209]);

    private static bool IsValid(string mode, int words) =>
        new AddMatchingActivityCommandValidator()
            .Validate(new AddMatchingActivityCommand(Guid.NewGuid(), mode, Enumerable.Range(0, words).Select(_ => Guid.NewGuid()).ToList(), Version))
            .IsValid;

    [Theory]
    [InlineData("WordToPicture", 2, true)]
    [InlineData("WordToMeaning", 6, true)]
    [InlineData("WordToPicture", 1, false)]
    [InlineData("WordToPicture", 7, false)]
    [InlineData("wordToPicture", 3, false)]
    [InlineData("1", 3, false)]
    [InlineData("", 3, false)]
    public void A_matching_has_a_known_mode_and_two_to_six_words(string mode, int words, bool valid)
    {
        Assert.Equal(valid, IsValid(mode, words));
    }

    [Fact]
    public void Words_are_different_and_the_lesson_version_is_needed()
    {
        var cat = Guid.NewGuid();

        Assert.False(new AddMatchingActivityCommandValidator()
            .Validate(new AddMatchingActivityCommand(Guid.NewGuid(), "WordToPicture", [cat, cat], Version)).IsValid);
        Assert.False(new AddMatchingActivityCommandValidator()
            .Validate(new AddMatchingActivityCommand(Guid.NewGuid(), "WordToPicture", [cat, Guid.Empty], Version)).IsValid);
        Assert.False(new UpdateMatchingActivityCommandValidator()
            .Validate(new UpdateMatchingActivityCommand(Guid.NewGuid(), Guid.NewGuid(), "WordToPicture", [cat, Guid.NewGuid()], "")).IsValid);
    }
}
