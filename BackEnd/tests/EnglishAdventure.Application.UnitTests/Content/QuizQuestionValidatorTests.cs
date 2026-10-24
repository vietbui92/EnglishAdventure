using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Commands.AddQuizActivity;
using EnglishAdventure.Application.Content.Commands.AddQuizQuestion;
using EnglishAdventure.Application.Content.Commands.ReorderQuizQuestions;
using EnglishAdventure.Application.Content.Commands.UpdateQuizQuestion;

namespace EnglishAdventure.Application.UnitTests.Content;

public sealed class QuizQuestionValidatorTests
{
    private static readonly string Version = ContentVersion.Encode([0, 0, 0, 0, 0, 0, 7, 209]);
    private static readonly Guid Cat = Guid.NewGuid();
    private static readonly Guid Dog = Guid.NewGuid();
    private static readonly Guid Fish = Guid.NewGuid();
    private static readonly Guid Bird = Guid.NewGuid();

    private static bool IsValid(string form, Guid correct, IReadOnlyList<Guid> wrong, string? sentence = null) =>
        new AddQuizQuestionCommandValidator()
            .Validate(new AddQuizQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), form, correct, wrong, sentence, Version))
            .IsValid;

    [Theory]
    [InlineData("PictureToWord")]
    [InlineData("WordToPicture")]
    public void A_question_has_two_to_four_options(string form)
    {
        Assert.True(IsValid(form, Cat, [Dog]));
        Assert.True(IsValid(form, Cat, [Dog, Fish]));
        Assert.True(IsValid(form, Cat, [Dog, Fish, Bird]));
        Assert.False(IsValid(form, Cat, []));
        Assert.False(IsValid(form, Cat, [Dog, Fish, Bird, Guid.NewGuid()]));
    }

    [Fact]
    public void Options_are_different_words_with_one_correct_answer()
    {
        Assert.False(IsValid("PictureToWord", Cat, [Dog, Dog]));
        Assert.False(IsValid("PictureToWord", Cat, [Cat, Dog]));
        Assert.False(IsValid("PictureToWord", Guid.Empty, [Dog]));
        Assert.False(IsValid("PictureToWord", Cat, [Guid.Empty]));
    }

    [Theory]
    [InlineData("pictureToWord")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData("Matching")]
    public void The_form_is_one_of_the_three_names(string form)
    {
        Assert.False(IsValid(form, Cat, [Dog]));
    }

    [Fact]
    public void A_fill_in_question_needs_a_sentence_with_one_gap_and_others_have_none()
    {
        Assert.True(IsValid("FillInTheBlank", Cat, [Dog], "I have a ___."));
        Assert.False(IsValid("FillInTheBlank", Cat, [Dog]));
        Assert.False(IsValid("FillInTheBlank", Cat, [Dog], "I have a cat."));
        Assert.False(IsValid("FillInTheBlank", Cat, [Dog], new string('a', 198) + "___"));
        Assert.False(IsValid("PictureToWord", Cat, [Dog], "I have a ___."));
    }

    [Fact]
    public void Quiz_changes_need_the_lesson_version()
    {
        Assert.False(new AddQuizActivityCommandValidator().Validate(new AddQuizActivityCommand(Guid.NewGuid(), "")).IsValid);
        Assert.False(new UpdateQuizQuestionCommandValidator()
            .Validate(new UpdateQuizQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PictureToWord", Cat, [Dog], null, "x"))
            .IsValid);
        Assert.False(new ReorderQuizQuestionsCommandValidator()
            .Validate(new ReorderQuizQuestionsCommand(Guid.NewGuid(), Guid.NewGuid(), [Cat, Cat], Version))
            .IsValid);
    }
}
