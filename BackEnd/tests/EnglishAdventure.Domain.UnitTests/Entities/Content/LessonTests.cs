using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class LessonTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

    private static Level NewLevel() => Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);

    private static Lesson NewLesson(Level level) => Lesson.Create(level, ContentTitle.From("Pets"), sortOrder: 1, Now);

    [Fact]
    public void A_new_lesson_is_a_draft_of_its_level()
    {
        var level = NewLevel();

        var lesson = NewLesson(level);

        Assert.NotEqual(Guid.Empty, lesson.Id);
        Assert.Equal(level.Id, lesson.LevelId);
        Assert.Equal(ContentStatus.Draft, lesson.Status);
        Assert.Equal("Pets", lesson.Title.Value);
    }

    [Fact]
    public void A_lesson_is_published_renamed_and_unpublished_while_no_child_started_it()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);

        lesson.Publish(level, Now);
        lesson.Rename(level, ContentTitle.From("My pets"));
        Assert.Equal(ContentStatus.Published, lesson.Status);
        Assert.Equal("My pets", lesson.Title.Value);

        Assert.Throws<DomainException>(() => lesson.Unpublish(level, learnersStarted: true));
        lesson.Unpublish(level, learnersStarted: false);
        Assert.Equal(ContentStatus.Draft, lesson.Status);
    }

    [Fact]
    public void An_archived_level_freezes_its_lessons_but_they_can_still_be_archived()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        level.Archive(Now);

        Assert.Throws<DomainException>(() => NewLesson(level));
        Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        Assert.Throws<DomainException>(() => lesson.Rename(level, ContentTitle.From("X")));
        Assert.Throws<DomainException>(() => lesson.MoveTo(level, 2));

        lesson.Archive(Now);
        Assert.Equal(ContentStatus.Archived, lesson.Status);
    }

    [Fact]
    public void An_archived_lesson_cannot_change()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        lesson.Archive(Now);

        Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        Assert.Throws<DomainException>(() => lesson.Rename(level, ContentTitle.From("X")));
        Assert.Throws<DomainException>(() => lesson.MoveTo(level, 2));
    }

    [Fact]
    public void A_lesson_is_changed_only_with_its_own_level()
    {
        var lesson = NewLesson(NewLevel());

        Assert.Throws<DomainException>(() => lesson.Publish(NewLevel(), Now));
    }
}
