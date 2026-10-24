using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Commands.AddLessonWord;
using EnglishAdventure.Application.Content.Commands.ReorderLessonWords;
using EnglishAdventure.Application.Content.Commands.SetLearnCard;
using EnglishAdventure.Application.Content.Commands.UpdateLessonWord;

namespace EnglishAdventure.Application.UnitTests.Content;

public sealed class LessonWordValidatorTests
{
    private static readonly string Version = ContentVersion.Encode([0, 0, 0, 0, 0, 0, 7, 209]);

    private static AddLessonWordCommand Add(string english = "cat", string meaning = "con mèo", string? imageUrl = null) =>
        new(Guid.NewGuid(), english, meaning, imageUrl, Version);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://cdn.example.test/cat.png")]
    public void A_word_may_be_saved_without_a_picture_or_with_an_https_one(string? imageUrl)
    {
        Assert.True(new AddLessonWordCommandValidator().Validate(Add(imageUrl: imageUrl)).IsValid);
    }

    [Theory]
    [InlineData("", "con mèo", null)]
    [InlineData("cat", "  ", null)]
    [InlineData("cat", "con mèo", "http://cdn.example.test/cat.png")]
    [InlineData("cat", "con mèo", "not a url")]
    public void Blank_texts_and_non_https_pictures_are_rejected(string english, string meaning, string? imageUrl)
    {
        Assert.False(new AddLessonWordCommandValidator().Validate(Add(english, meaning, imageUrl)).IsValid);
        Assert.False(new UpdateLessonWordCommandValidator()
            .Validate(new UpdateLessonWordCommand(Guid.NewGuid(), Guid.NewGuid(), english, meaning, imageUrl, Version))
            .IsValid);
    }

    [Fact]
    public void Texts_have_their_maximum_length()
    {
        var validator = new AddLessonWordCommandValidator();

        Assert.True(validator.Validate(Add(new string('a', 50), new string('b', 100))).IsValid);
        Assert.False(validator.Validate(Add(new string('a', 51))).IsValid);
        Assert.False(validator.Validate(Add(meaning: new string('b', 101))).IsValid);
    }

    [Fact]
    public void Word_changes_need_the_lesson_version()
    {
        Assert.False(new AddLessonWordCommandValidator()
            .Validate(new AddLessonWordCommand(Guid.NewGuid(), "cat", "con mèo", null, "")).IsValid);
        Assert.False(new ReorderLessonWordsCommandValidator()
            .Validate(new ReorderLessonWordsCommand(Guid.NewGuid(), [Guid.NewGuid()], "nope")).IsValid);
        Assert.True(new ReorderLessonWordsCommandValidator()
            .Validate(new ReorderLessonWordsCommand(Guid.NewGuid(), [Guid.NewGuid()], Version)).IsValid);
        Assert.False(new SetLearnCardCommandValidator()
            .Validate(new SetLearnCardCommand(Guid.NewGuid(), Guid.NewGuid(), false, "")).IsValid);
        Assert.True(new SetLearnCardCommandValidator()
            .Validate(new SetLearnCardCommand(Guid.NewGuid(), Guid.NewGuid(), false, Version)).IsValid);
    }
}
