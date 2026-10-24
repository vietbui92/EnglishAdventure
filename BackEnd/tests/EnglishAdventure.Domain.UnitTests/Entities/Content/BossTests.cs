using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class BossTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/lion.png");

    private readonly Level _level = Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);
    private readonly Guid _cat = Guid.NewGuid();
    private readonly Guid _dog = Guid.NewGuid();
    private readonly Guid _bare = Guid.NewGuid();
    private readonly LevelWords _levelWords;

    public BossTests()
    {
        _levelWords = LevelWords.From([(_cat, true), (_dog, true), (_bare, false)]);
    }

    private Lesson NewBoss(ImageUrl? picture = null) =>
        Lesson.CreateBoss(_level, ContentTitle.From("King Lion"), picture ?? Picture, Now);

    /// <summary>A boss with a picture and a matching of two level words, ready to publish.</summary>
    private Lesson ReadyBoss()
    {
        var boss = NewBoss();
        boss.AddMatching(_level, MatchingMode.WordToPicture, [_cat, _dog], Now, _levelWords);
        return boss;
    }

    [Fact]
    public void A_boss_is_a_lesson_of_its_own_kind_with_a_name_and_a_picture()
    {
        var boss = NewBoss();

        Assert.True(boss.IsBoss);
        Assert.Equal(LessonKind.Boss, boss.Kind);
        Assert.Equal("King Lion", boss.Title.Value);
        Assert.Equal(Picture, boss.BossImageUrl);
        Assert.Equal(ContentStatus.Draft, boss.Status);
        Assert.Equal(LessonKind.Regular, Lesson.Create(_level, ContentTitle.From("Pets"), 1, Now).Kind);
    }

    [Fact]
    public void A_boss_has_no_word_bank_and_no_learn_step()
    {
        var boss = NewBoss();

        var error = Assert.Throws<DomainException>(() => boss.AddWord(_level, "cat", "con mèo", Picture, Now));

        Assert.Equal(ContentErrorCodes.BossHasNoWordBank, error.Code);
        Assert.Empty(boss.LearnCards);
    }

    [Fact]
    public void A_boss_builds_its_activities_from_the_level_words()
    {
        var boss = NewBoss();
        var quiz = boss.AddQuiz(_level, Now);

        boss.AddQuizQuestion(
            _level,
            quiz.Id,
            QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, _cat, [_dog], null),
            Now,
            _levelWords);
        var stranger = Assert.Throws<DomainException>(() => boss.AddMatching(
            _level,
            MatchingMode.WordToPicture,
            [_cat, Guid.NewGuid()],
            Now,
            _levelWords));

        Assert.True(boss.UsesWord(_cat));
        Assert.False(boss.UsesWord(_bare));
        Assert.Equal(ContentErrorCodes.InvalidMatching, stranger.Code);
    }

    [Fact]
    public void A_boss_is_published_with_its_picture_an_activity_and_pictures_for_the_words_it_uses()
    {
        var noPicture = Lesson.CreateBoss(_level, ContentTitle.From("King Lion"), null, Now);
        noPicture.AddMatching(_level, MatchingMode.WordToPicture, [_cat, _dog], Now, _levelWords);
        var noActivity = NewBoss();
        var wordWithoutPicture = NewBoss();
        wordWithoutPicture.AddMatching(_level, MatchingMode.WordToMeaning, [_cat, _bare], Now, _levelWords);
        var ready = ReadyBoss();

        Assert.Equal(
            ContentErrorCodes.LessonNotReady,
            Assert.Throws<DomainException>(() => noPicture.Publish(_level, Now, _levelWords)).Code);
        Assert.Equal(
            ContentErrorCodes.LessonNotReady,
            Assert.Throws<DomainException>(() => noActivity.Publish(_level, Now, _levelWords)).Code);
        Assert.Equal(
            ContentErrorCodes.LessonNotReady,
            Assert.Throws<DomainException>(() => wordWithoutPicture.Publish(_level, Now, _levelWords)).Code);
        ready.Publish(_level, Now, _levelWords);
        Assert.Equal(ContentStatus.Published, ready.Status);
    }

    [Fact]
    public void A_published_boss_is_renamed_and_keeps_a_picture()
    {
        var boss = ReadyBoss();
        boss.Publish(_level, Now, _levelWords);

        boss.UpdateBoss(_level, ContentTitle.From("Lion King"), ImageUrl.From("https://cdn.example.test/king.png"));
        var dropPicture = Assert.Throws<DomainException>(() => boss.UpdateBoss(_level, ContentTitle.From("Lion King"), null));

        Assert.Equal("Lion King", boss.Title.Value);
        Assert.Equal(ContentErrorCodes.LessonNotReady, dropPicture.Code);
        Assert.Throws<DomainException>(
            () => Lesson.Create(_level, ContentTitle.From("Pets"), 1, Now).UpdateBoss(_level, ContentTitle.From("X"), Picture));
    }

    [Fact]
    public void A_level_is_published_only_with_a_published_boss_and_then_keeps_it()
    {
        var boss = ReadyBoss();

        var noBoss = Assert.Throws<DomainException>(() => _level.Publish(Now, hasPublishedBoss: false));
        boss.Publish(_level, Now, _levelWords);
        _level.Publish(Now, hasPublishedBoss: true);

        Assert.Equal(ContentErrorCodes.LevelNeedsBoss, noBoss.Code);
        Assert.Equal(
            ContentErrorCodes.BossRequired,
            Assert.Throws<DomainException>(() => boss.Unpublish(_level, learnersStarted: false)).Code);
        Assert.Equal(
            ContentErrorCodes.BossRequired,
            Assert.Throws<DomainException>(() => boss.Archive(_level, Now)).Code);

        _level.Unpublish(learnersStarted: false);
        boss.Unpublish(_level, learnersStarted: false);
        boss.Archive(_level, Now);
        Assert.Equal(ContentStatus.Archived, boss.Status);
    }

    [Fact]
    public void A_lesson_keeps_a_word_the_boss_uses()
    {
        var lesson = Lesson.Create(_level, ContentTitle.From("Pets"), 1, Now);
        var cat = lesson.AddWord(_level, "cat", "con mèo", Picture, Now);

        var error = Assert.Throws<DomainException>(() => lesson.RemoveWord(_level, cat.Id, Now, usedByBoss: true));
        lesson.RemoveWord(_level, cat.Id, Now, usedByBoss: false);

        Assert.Equal(ContentErrorCodes.WordInUse, error.Code);
        Assert.Empty(lesson.Words);
    }
}
