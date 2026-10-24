using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.Services;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Learning;

public sealed class LessonAttemptTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/picture.png");

    private readonly Guid _child = Guid.NewGuid();
    private readonly Lesson _lesson;
    private readonly QuizQuestion _catQuestion;
    private readonly QuizQuestion _dogQuestion;
    private readonly MatchingPair _catPair;
    private readonly MatchingPair _dogPair;
    private readonly AnswerKey _key;

    /// <summary>A lesson of two quiz questions and a matching of two pairs: four scored items.</summary>
    public LessonAttemptTests()
    {
        var level = Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);
        _lesson = Lesson.Create(level, ContentTitle.From("Pets"), sortOrder: 1, Now);
        var cat = _lesson.AddWord(level, "cat", "con mèo", Picture, Now);
        var dog = _lesson.AddWord(level, "dog", "con chó", Picture, Now);
        var quiz = _lesson.AddQuiz(level, Now);
        _catQuestion = _lesson.AddQuizQuestion(level, quiz.Id, QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, cat.Id, [dog.Id], null), Now);
        _dogQuestion = _lesson.AddQuizQuestion(level, quiz.Id, QuizQuestionSpec.Create(QuizQuestionForm.WordToPicture, dog.Id, [cat.Id], null), Now);
        var matching = _lesson.AddMatching(level, MatchingMode.WordToPicture, [cat.Id, dog.Id], Now);
        _catPair = matching.Pairs[0];
        _dogPair = matching.Pairs[1];
        _key = AnswerKey.For(_lesson);
    }

    private LessonAttempt NewAttempt() => LessonAttempt.Start(_child, _lesson.Id, _key, Now);

    private static Guid Correct(QuizQuestion question) => question.Options.Single(question.IsCorrect).Id;

    private static Guid Wrong(QuizQuestion question) => question.Options.First(option => !question.IsCorrect(option)).Id;

    [Fact]
    public void The_answer_key_scores_every_question_and_pair_and_keeps_both_sides_apart()
    {
        Assert.Equal(4, _key.TotalItems);
        Assert.Equal(Correct(_catQuestion), _key.Question(_catQuestion.Id)!.CorrectOptionId);
        Assert.Equal(_catPair.ChoiceId, _key.Pair(_catPair.Id)!.RightId);
        Assert.NotEqual(_catPair.Id, _catPair.ChoiceId);
    }

    [Fact]
    public void A_new_attempt_is_in_progress_for_its_child()
    {
        var attempt = NewAttempt();

        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(4, attempt.TotalItems);
        Assert.True(attempt.IsFor(_child));
        Assert.False(attempt.IsFor(Guid.NewGuid()));
        Assert.Null(attempt.Stars);
    }

    [Fact]
    public void A_question_keeps_its_first_answer_whatever_is_sent_again()
    {
        var attempt = NewAttempt();

        var first = attempt.AnswerQuestion(_key, _catQuestion.Id, Wrong(_catQuestion), Now);
        var again = attempt.AnswerQuestion(_key, _catQuestion.Id, Correct(_catQuestion), Now);

        Assert.False(first.Correct);
        Assert.Equal(Correct(_catQuestion), first.CorrectChoiceId);
        Assert.False(first.Repeated);
        Assert.False(again.Correct);
        Assert.True(again.Repeated);
        Assert.Single(attempt.Answers);
    }

    [Fact]
    public void Unknown_questions_options_and_partners_are_refused()
    {
        var attempt = NewAttempt();

        Assert.Equal(
            LearningErrorCodes.InvalidAnswer,
            Assert.Throws<DomainException>(() => attempt.AnswerQuestion(_key, Guid.NewGuid(), Correct(_catQuestion), Now)).Code);
        // An option of another question.
        Assert.Equal(
            LearningErrorCodes.InvalidAnswer,
            Assert.Throws<DomainException>(() => attempt.AnswerQuestion(_key, _catQuestion.Id, Correct(_dogQuestion), Now)).Code);
        Assert.Throws<DomainException>(() => attempt.MatchPair(_key, _catPair.Id, Guid.NewGuid(), Now));
        Assert.Throws<DomainException>(() => attempt.MatchPair(_key, Guid.NewGuid(), _catPair.ChoiceId, Now));
        Assert.Empty(attempt.Answers);
    }

    [Fact]
    public void A_pair_is_tried_until_right_and_counts_only_when_right_first()
    {
        var attempt = NewAttempt();

        var wrong = attempt.MatchPair(_key, _catPair.Id, _dogPair.ChoiceId, Now);
        var right = attempt.MatchPair(_key, _catPair.Id, _catPair.ChoiceId, Now);
        var again = attempt.MatchPair(_key, _catPair.Id, _dogPair.ChoiceId, Now);

        Assert.False(wrong.Correct);
        Assert.Null(wrong.CorrectChoiceId);
        Assert.True(right.Correct);
        Assert.True(again.Repeated);
        Assert.True(again.Correct);
        Assert.Equal([1, 2], attempt.Answers.Select(answer => answer.TryNumber));
    }

    [Fact]
    public void An_attempt_finishes_with_its_score_and_stars_when_everything_is_done()
    {
        var attempt = NewAttempt();
        attempt.AnswerQuestion(_key, _catQuestion.Id, Correct(_catQuestion), Now);
        attempt.AnswerQuestion(_key, _dogQuestion.Id, Correct(_dogQuestion), Now);
        attempt.MatchPair(_key, _catPair.Id, _dogPair.ChoiceId, Now);
        attempt.MatchPair(_key, _catPair.Id, _catPair.ChoiceId, Now);
        Assert.Equal(AttemptStatus.InProgress, attempt.Status);

        attempt.MatchPair(_key, _dogPair.Id, _dogPair.ChoiceId, Now.AddMinutes(3));

        // 3 of 4 right on the first try: 75%, one star.
        Assert.Equal(AttemptStatus.Finished, attempt.Status);
        Assert.Equal(3, attempt.CorrectItems);
        Assert.Equal(75, attempt.ScorePercent);
        Assert.Equal(1, attempt.Stars);
        Assert.Equal(Now.AddMinutes(3), attempt.FinishedAtUtc);
    }

    [Fact]
    public void A_finished_attempt_keeps_its_answers()
    {
        var attempt = NewAttempt();
        attempt.AnswerQuestion(_key, _catQuestion.Id, Correct(_catQuestion), Now);
        attempt.AnswerQuestion(_key, _dogQuestion.Id, Correct(_dogQuestion), Now);
        attempt.MatchPair(_key, _catPair.Id, _catPair.ChoiceId, Now);
        attempt.MatchPair(_key, _dogPair.Id, _dogPair.ChoiceId, Now);

        var repeated = attempt.AnswerQuestion(_key, _catQuestion.Id, Wrong(_catQuestion), Now);

        Assert.Equal(3, attempt.Stars);
        Assert.True(repeated.Repeated);
        Assert.True(repeated.Correct);
        Assert.Equal(4, attempt.Answers.Count);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(59, 0)]
    [InlineData(60, 1)]
    [InlineData(79, 1)]
    [InlineData(80, 2)]
    [InlineData(99, 2)]
    [InlineData(100, 3)]
    public void Stars_follow_the_60_80_100_thresholds(int percent, int stars)
    {
        Assert.Equal(stars, LessonScoring.Stars(percent));
    }

    [Fact]
    public void A_score_rounds_down_so_almost_is_not_enough()
    {
        Assert.Equal(66, LessonScoring.Percent(2, 3));
        Assert.Equal(59, LessonScoring.Percent(59, 100));
        Assert.Equal(0, LessonScoring.Percent(1, 0));
    }
}
