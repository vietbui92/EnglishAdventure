using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.Services;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Learning;

public sealed class LessonCompletionTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/picture.png");

    private readonly Guid _child = Guid.NewGuid();
    private readonly Lesson _lesson;
    private readonly List<QuizQuestion> _questions = new();
    private readonly AnswerKey _key;

    /// <summary>A lesson of five quiz questions: each right answer is 20%.</summary>
    public LessonCompletionTests()
    {
        var level = Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);
        _lesson = Lesson.Create(level, ContentTitle.From("Pets"), sortOrder: 1, Now);
        var words = new[] { "cat", "dog", "fish", "bird", "frog" }
            .Select(english => _lesson.AddWord(level, english, "nghĩa", Picture, Now))
            .ToList();
        var quiz = _lesson.AddQuiz(level, Now);
        for (var index = 0; index < words.Count; index++)
        {
            var wrong = words[(index + 1) % words.Count];
            _questions.Add(_lesson.AddQuizQuestion(
                level,
                quiz.Id,
                QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, words[index].Id, [wrong.Id], null),
                Now));
        }

        _key = AnswerKey.For(_lesson);
    }

    /// <summary>A finished attempt with the first <paramref name="right"/> answers right and the rest wrong.</summary>
    private LessonAttempt Played(int right, int minutesLater = 0)
    {
        var at = Now.AddMinutes(minutesLater);
        var attempt = LessonAttempt.Start(_child, _lesson.Id, _key, at);
        for (var index = 0; index < _questions.Count; index++)
        {
            var question = _questions[index];
            var option = question.Options.First(candidate => question.IsCorrect(candidate) == index < right);
            attempt.AnswerQuestion(_key, question.Id, option.Id, at);
        }

        return attempt;
    }

    [Fact]
    public void A_finished_attempt_with_a_star_completes_the_lesson()
    {
        var attempt = Played(right: 4);

        var completion = LessonCompletion.From(attempt);

        Assert.True(LessonCompletion.Completes(attempt));
        Assert.Equal(2, completion.BestStars);
        Assert.Equal(80, completion.BestScorePercent);
        Assert.Equal(_child, completion.ChildProfileId);
        Assert.Equal(_lesson.Id, completion.LessonId);
        Assert.Equal(Now, completion.CompletedAtUtc);
    }

    [Fact]
    public void No_star_or_an_unfinished_attempt_does_not_complete_it()
    {
        var unfinished = LessonAttempt.Start(_child, _lesson.Id, _key, Now);

        Assert.False(LessonCompletion.Completes(Played(right: 2)));
        Assert.False(LessonCompletion.Completes(unfinished));
        Assert.Throws<DomainException>(() => LessonCompletion.From(Played(right: 2)));
    }

    [Fact]
    public void Replays_only_raise_the_best_result()
    {
        var completion = LessonCompletion.From(Played(right: 3));

        Assert.False(completion.Improve(Played(right: 1, minutesLater: 5)));
        Assert.Equal(1, completion.BestStars);
        Assert.True(completion.Improve(Played(right: 5, minutesLater: 10)));
        Assert.False(completion.Improve(Played(right: 4, minutesLater: 15)));

        Assert.Equal(3, completion.BestStars);
        Assert.Equal(100, completion.BestScorePercent);
        Assert.Equal(Now, completion.CompletedAtUtc);
        Assert.Equal(Now.AddMinutes(10), completion.BestAtUtc);
    }

    [Fact]
    public void Only_the_same_child_and_lesson_can_improve_it()
    {
        var completion = LessonCompletion.From(Played(right: 3));
        var other = LessonAttempt.Start(Guid.NewGuid(), _lesson.Id, _key, Now);

        Assert.Throws<DomainException>(() => completion.Improve(other));
        Assert.Throws<DomainException>(() => completion.Improve(LessonAttempt.Start(_child, _lesson.Id, _key, Now)));
    }
}
