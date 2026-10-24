using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class MatchingTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/picture.png");

    private readonly Level _level = Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);
    private readonly Lesson _lesson;
    private readonly List<LessonWord> _words;

    public MatchingTests()
    {
        _lesson = Lesson.Create(_level, ContentTitle.From("Pets"), sortOrder: 1, Now);
        _words = new[] { "cat", "dog", "fish", "bird", "cow", "pig", "duck" }
            .Select(english => _lesson.AddWord(_level, english, "nghĩa", Picture, Now))
            .ToList();
    }

    private Guid[] Ids(int count) => _words.Take(count).Select(word => word.Id).ToArray();

    [Fact]
    public void A_matching_pairs_two_to_six_words_in_the_Admins_order()
    {
        var matching = _lesson.AddMatching(_level, MatchingMode.WordToPicture, [_words[2].Id, _words[0].Id], Now);

        Assert.Equal(ActivityType.Matching, matching.Type);
        Assert.Equal(MatchingMode.WordToPicture, matching.Mode);
        Assert.Equal([_words[2].Id, _words[0].Id], matching.Pairs.Select(pair => pair.WordId));
        Assert.Equal(6, MatchingActivity.MaxPairs);
        _lesson.AddMatching(_level, MatchingMode.WordToMeaning, Ids(6), Now);
    }

    [Fact]
    public void A_matching_with_too_few_too_many_or_repeated_words_is_invalid()
    {
        Assert.Equal(
            ContentErrorCodes.InvalidMatching,
            Assert.Throws<DomainException>(() => _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(1), Now)).Code);
        Assert.Throws<DomainException>(() => _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(7), Now));
        Assert.Throws<DomainException>(
            () => _lesson.AddMatching(_level, MatchingMode.WordToPicture, [_words[0].Id, _words[0].Id], Now));
        Assert.Throws<DomainException>(() => _lesson.AddMatching(_level, (MatchingMode)9, Ids(2), Now));
        Assert.Empty(_lesson.Activities);
    }

    [Fact]
    public void Every_pair_is_a_word_of_the_lesson()
    {
        var error = Assert.Throws<DomainException>(
            () => _lesson.AddMatching(_level, MatchingMode.WordToPicture, [_words[0].Id, Guid.NewGuid()], Now));

        Assert.Equal(ContentErrorCodes.InvalidMatching, error.Code);
    }

    [Fact]
    public void A_matching_changes_its_mode_and_words_and_keeps_the_pairs_that_stay()
    {
        var matching = _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(3), Now);
        var kept = matching.Pairs.Single(pair => pair.WordId == _words[1].Id);

        _lesson.UpdateMatching(_level, matching.Id, MatchingMode.WordToMeaning, [_words[1].Id, _words[4].Id], Now.AddMinutes(2));

        Assert.Equal(MatchingMode.WordToMeaning, matching.Mode);
        Assert.Equal([_words[1].Id, _words[4].Id], matching.Pairs.Select(pair => pair.WordId));
        Assert.Same(kept, matching.Pairs[0]);
        Assert.Equal([1, 2], matching.Pairs.Select(pair => pair.SortOrder));
        Assert.Equal(Now.AddMinutes(2), _lesson.ContentChangedAtUtc);
    }

    [Fact]
    public void A_lesson_with_only_a_matching_can_be_published_and_then_locks_it()
    {
        var matching = _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(2), Now);
        _lesson.Publish(_level, Now);

        var change = Assert.Throws<DomainException>(
            () => _lesson.UpdateMatching(_level, matching.Id, MatchingMode.WordToMeaning, Ids(2), Now));

        Assert.Equal(ContentStatus.Published, _lesson.Status);
        Assert.Equal(ContentErrorCodes.StructureLocked, change.Code);
        Assert.Equal(
            ContentErrorCodes.StructureLocked,
            Assert.Throws<DomainException>(() => _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(2), Now)).Code);
    }

    [Fact]
    public void Quizzes_and_matchings_share_the_activity_limit_and_words_in_a_matching_stay()
    {
        _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(2), Now);
        _lesson.AddQuiz(_level, Now);
        _lesson.AddMatching(_level, MatchingMode.WordToMeaning, Ids(3), Now);
        _lesson.AddQuiz(_level, Now);

        Assert.Equal(
            ContentErrorCodes.ActivityLimit,
            Assert.Throws<DomainException>(() => _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(2), Now)).Code);
        Assert.Equal(
            ContentErrorCodes.WordInUse,
            Assert.Throws<DomainException>(() => _lesson.RemoveWord(_level, _words[0].Id, Now)).Code);
        _lesson.RemoveWord(_level, _words[6].Id, Now);
    }

    [Fact]
    public void Quiz_question_endpoints_do_not_apply_to_a_matching()
    {
        var matching = _lesson.AddMatching(_level, MatchingMode.WordToPicture, Ids(2), Now);
        var spec = QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, _words[0].Id, [_words[1].Id], null);

        Assert.Throws<DomainException>(() => _lesson.AddQuizQuestion(_level, matching.Id, spec, Now));
    }
}
