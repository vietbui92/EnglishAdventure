using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class QuizTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/picture.png");

    private readonly Level _level = Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);
    private readonly Lesson _lesson;
    private readonly LessonWord _cat;
    private readonly LessonWord _dog;
    private readonly LessonWord _fish;

    public QuizTests()
    {
        _lesson = Lesson.Create(_level, ContentTitle.From("Pets"), sortOrder: 1, Now);
        _cat = _lesson.AddWord(_level, "cat", "con mèo", Picture, Now);
        _dog = _lesson.AddWord(_level, "dog", "con chó", Picture, Now);
        _fish = _lesson.AddWord(_level, "fish", "con cá", Picture, Now);
    }

    private QuizQuestionSpec PictureQuestion(LessonWord correct, params LessonWord[] wrong) =>
        QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, correct.Id, wrong.Select(word => word.Id).ToList(), null);

    [Fact]
    public void A_quiz_is_added_after_the_other_activities_and_its_questions_keep_their_order()
    {
        var first = _lesson.AddQuiz(_level, Now);
        var second = _lesson.AddQuiz(_level, Now.AddMinutes(1));

        _lesson.AddQuizQuestion(_level, first.Id, PictureQuestion(_cat, _dog, _fish), Now.AddMinutes(1));
        _lesson.AddQuizQuestion(_level, first.Id, PictureQuestion(_dog, _cat), Now.AddMinutes(1));

        Assert.Equal([first.Id, second.Id], _lesson.Activities.Select(activity => activity.Id));
        Assert.Equal(ActivityType.Quiz, first.Type);
        Assert.Equal([_cat.Id, _dog.Id], first.Questions.Select(question => question.CorrectWordId));
        Assert.Equal(3, first.Questions[0].Options.Count);
        Assert.Single(first.Questions[0].Options, option => first.Questions[0].IsCorrect(option));
        Assert.Equal(Now.AddMinutes(1), _lesson.ContentChangedAtUtc);
    }

    [Fact]
    public void A_lesson_has_at_most_four_activities_and_a_quiz_at_most_ten_questions()
    {
        for (var index = 0; index < Lesson.MaxActivities; index++)
        {
            _lesson.AddQuiz(_level, Now);
        }

        var quiz = (QuizActivity)_lesson.Activities[0];
        for (var index = 0; index < QuizActivity.MaxQuestions; index++)
        {
            _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_cat, _dog), Now);
        }

        Assert.Equal(4, Lesson.MaxActivities);
        Assert.Equal(10, QuizActivity.MaxQuestions);
        Assert.Equal(ContentErrorCodes.ActivityLimit, Assert.Throws<DomainException>(() => _lesson.AddQuiz(_level, Now)).Code);
        Assert.Equal(
            ContentErrorCodes.QuestionLimit,
            Assert.Throws<DomainException>(() => _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_cat, _dog), Now)).Code);
    }

    [Fact]
    public void A_question_has_two_to_four_different_options_and_exactly_one_correct()
    {
        Assert.Throws<DomainException>(() => PictureQuestion(_cat));
        Assert.Throws<DomainException>(() => PictureQuestion(_cat, _dog, _fish, _lesson.AddWord(_level, "bird", "chim", Picture, Now), _lesson.AddWord(_level, "cow", "bò", Picture, Now)));
        Assert.Throws<DomainException>(() => PictureQuestion(_cat, _dog, _dog));
        Assert.Throws<DomainException>(() => PictureQuestion(_cat, _cat));
        Assert.Equal(4, PictureQuestion(_cat, _dog, _fish, _lesson.Words.Last()).WrongWordIds.Count + 1);
    }

    [Theory]
    [InlineData("I have a ___.", true)]
    [InlineData("  The ___ says meow. ", true)]
    [InlineData("I have a cat.", false)]
    [InlineData("A ___ and a ___.", false)]
    [InlineData("I have a ____.", false)]
    [InlineData("", false)]
    public void A_fill_in_sentence_has_exactly_one_gap(string sentence, bool valid)
    {
        Assert.Equal(valid, QuizQuestionSpec.IsValidSentence(sentence));
        if (valid)
        {
            var spec = QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _cat.Id, [_dog.Id], sentence);
            Assert.Equal(sentence.Trim(), spec.Sentence);
        }
        else
        {
            Assert.Throws<DomainException>(() => QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _cat.Id, [_dog.Id], sentence));
        }
    }

    [Fact]
    public void Only_a_fill_in_question_has_a_sentence()
    {
        var error = Assert.Throws<DomainException>(
            () => QuizQuestionSpec.Create(QuizQuestionForm.WordToPicture, _cat.Id, [_dog.Id], "I have a ___."));

        Assert.Equal(ContentErrorCodes.InvalidQuestion, error.Code);
        Assert.Null(QuizQuestionSpec.Create(QuizQuestionForm.WordToPicture, _cat.Id, [_dog.Id], "  ").Sentence);
    }

    [Fact]
    public void Every_option_is_a_word_of_the_lesson()
    {
        var quiz = _lesson.AddQuiz(_level, Now);
        var stranger = QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, _cat.Id, [Guid.NewGuid()], null);

        var error = Assert.Throws<DomainException>(() => _lesson.AddQuizQuestion(_level, quiz.Id, stranger, Now));

        Assert.Equal(ContentErrorCodes.InvalidQuestion, error.Code);
    }

    [Fact]
    public void A_word_used_by_a_question_cannot_be_removed()
    {
        var quiz = _lesson.AddQuiz(_level, Now);
        _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_cat, _dog), Now);

        Assert.Equal(ContentErrorCodes.WordInUse, Assert.Throws<DomainException>(() => _lesson.RemoveWord(_level, _dog.Id, Now)).Code);
        _lesson.RemoveWord(_level, _fish.Id, Now);
        Assert.Equal(2, _lesson.Words.Count);
    }

    [Fact]
    public void Questions_are_changed_removed_and_reordered_in_a_draft()
    {
        var quiz = _lesson.AddQuiz(_level, Now);
        var first = _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_cat, _dog), Now);
        var second = _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_dog, _cat), Now);
        var third = _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_fish, _cat), Now);

        _lesson.UpdateQuizQuestion(
            _level,
            quiz.Id,
            first.Id,
            QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _cat.Id, [_fish.Id, _dog.Id], "I have a ___."),
            Now);
        _lesson.RemoveQuizQuestion(_level, quiz.Id, second.Id, Now);
        _lesson.ReorderQuizQuestions(_level, quiz.Id, [third.Id, first.Id], Now);

        Assert.Equal(QuizQuestionForm.FillInTheBlank, first.Form);
        Assert.Equal([_cat.Id, _fish.Id, _dog.Id], first.Options.Select(option => option.WordId));
        Assert.Equal([third.Id, first.Id], quiz.Questions.Select(question => question.Id));
        Assert.Equal([1, 2], quiz.Questions.Select(question => question.SortOrder));
    }

    [Fact]
    public void Activities_are_removed_and_reordered_in_a_draft()
    {
        var first = _lesson.AddQuiz(_level, Now);
        var second = _lesson.AddQuiz(_level, Now);
        var third = _lesson.AddQuiz(_level, Now);

        _lesson.RemoveActivity(_level, second.Id, Now);
        _lesson.ReorderActivities(_level, [third.Id, first.Id], Now);

        Assert.Equal([third.Id, first.Id], _lesson.Activities.Select(activity => activity.Id));
        Assert.Equal([1, 2], _lesson.Activities.Select(activity => activity.SortOrder));
        Assert.Throws<DomainException>(() => _lesson.ReorderActivities(_level, [first.Id], Now));
    }

    [Fact]
    public void A_lesson_is_published_only_with_an_activity_and_no_empty_quiz()
    {
        var noActivity = Assert.Throws<DomainException>(() => _lesson.Publish(_level, Now));
        var quiz = _lesson.AddQuiz(_level, Now);
        var emptyQuiz = Assert.Throws<DomainException>(() => _lesson.Publish(_level, Now));
        _lesson.AddQuizQuestion(_level, quiz.Id, PictureQuestion(_cat, _dog), Now);
        _lesson.Publish(_level, Now);

        Assert.Equal(ContentErrorCodes.LessonNotReady, noActivity.Code);
        Assert.Equal(ContentErrorCodes.LessonNotReady, emptyQuiz.Code);
        Assert.Equal(ContentStatus.Published, _lesson.Status);
    }

    [Fact]
    public void A_published_quiz_corrects_only_its_sentences()
    {
        var quiz = _lesson.AddQuiz(_level, Now);
        var fill = QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _cat.Id, [_dog.Id], "I has a ___.");
        var question = _lesson.AddQuizQuestion(_level, quiz.Id, fill, Now);
        _lesson.Publish(_level, Now);

        _lesson.UpdateQuizQuestion(
            _level,
            quiz.Id,
            question.Id,
            QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _cat.Id, [_dog.Id], "I have a ___."),
            Now);
        var changeAnswer = Assert.Throws<DomainException>(() => _lesson.UpdateQuizQuestion(
            _level,
            quiz.Id,
            question.Id,
            QuizQuestionSpec.Create(QuizQuestionForm.FillInTheBlank, _dog.Id, [_cat.Id], "I have a ___."),
            Now));

        Assert.Equal("I have a ___.", question.Sentence);
        Assert.Equal(ContentErrorCodes.StructureLocked, changeAnswer.Code);
        Assert.Equal(ContentErrorCodes.StructureLocked, Assert.Throws<DomainException>(() => _lesson.AddQuiz(_level, Now)).Code);
        Assert.Equal(
            ContentErrorCodes.StructureLocked,
            Assert.Throws<DomainException>(() => _lesson.RemoveQuizQuestion(_level, quiz.Id, question.Id, Now)).Code);
        Assert.Equal(
            ContentErrorCodes.StructureLocked,
            Assert.Throws<DomainException>(() => _lesson.RemoveActivity(_level, quiz.Id, Now)).Code);
    }
}
