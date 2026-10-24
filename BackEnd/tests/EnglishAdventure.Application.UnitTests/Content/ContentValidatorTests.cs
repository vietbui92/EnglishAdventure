using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Commands.CreateLesson;
using EnglishAdventure.Application.Content.Commands.CreateLevel;
using EnglishAdventure.Application.Content.Commands.PublishLesson;
using EnglishAdventure.Application.Content.Commands.ReorderLevels;
using EnglishAdventure.Application.Content.Commands.UpdateLevel;

namespace EnglishAdventure.Application.UnitTests.Content;

public sealed class ContentValidatorTests
{
    private static readonly string ValidVersion = ContentVersion.Encode([0, 0, 0, 0, 0, 0, 7, 209]);

    [Theory]
    [InlineData("Animal Island", null)]
    [InlineData("  Pets  ", "")]
    [InlineData("Colors", "A level about colors")]
    public void A_level_needs_a_title_and_at_most_500_characters_of_description(string title, string? description)
    {
        Assert.True(new CreateLevelCommandValidator().Validate(new CreateLevelCommand(title, description)).IsValid);
    }

    [Fact]
    public void Blank_or_too_long_texts_are_rejected()
    {
        var levels = new CreateLevelCommandValidator();
        var lessons = new CreateLessonCommandValidator();

        Assert.False(levels.Validate(new CreateLevelCommand("  ", null)).IsValid);
        Assert.False(levels.Validate(new CreateLevelCommand(new string('a', 101), null)).IsValid);
        Assert.False(levels.Validate(new CreateLevelCommand("Pets", new string('a', 501))).IsValid);
        Assert.False(lessons.Validate(new CreateLessonCommand(Guid.NewGuid(), "")).IsValid);
        Assert.True(lessons.Validate(new CreateLessonCommand(Guid.NewGuid(), new string('a', 100))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAA")]
    [InlineData("AAAAAAAAAAAAAA==")]
    public void A_change_needs_the_version_returned_with_the_item(string version)
    {
        Assert.False(new PublishLessonCommandValidator().Validate(new PublishLessonCommand(Guid.NewGuid(), version)).IsValid);
        Assert.False(new UpdateLevelCommandValidator()
            .Validate(new UpdateLevelCommand(Guid.NewGuid(), "Pets", null, version)).IsValid);
    }

    [Fact]
    public void A_version_round_trips()
    {
        var rowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 7, 209 };

        Assert.True(new PublishLessonCommandValidator().Validate(new PublishLessonCommand(Guid.NewGuid(), ValidVersion)).IsValid);
        Assert.Equal(rowVersion, ContentVersion.Decode(ContentVersion.Encode(rowVersion)));
    }

    [Fact]
    public void An_order_lists_each_item_once()
    {
        var validator = new ReorderLevelsCommandValidator();
        var id = Guid.NewGuid();

        Assert.True(validator.Validate(new ReorderLevelsCommand([id, Guid.NewGuid()])).IsValid);
        Assert.True(validator.Validate(new ReorderLevelsCommand([])).IsValid);
        Assert.False(validator.Validate(new ReorderLevelsCommand([id, id])).IsValid);
        Assert.False(validator.Validate(new ReorderLevelsCommand([Guid.Empty])).IsValid);
        Assert.False(validator.Validate(new ReorderLevelsCommand(null!)).IsValid);
    }
}
