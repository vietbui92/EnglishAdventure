using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Commands.CreateLesson;
using EnglishAdventure.Application.Content.Commands.CreateLevel;
using EnglishAdventure.Application.Content.Commands.ReorderLessons;
using EnglishAdventure.Application.Content.Commands.ReorderLevels;
using EnglishAdventure.Application.Content.Commands.UnpublishLesson;
using EnglishAdventure.Application.Content.Commands.UnpublishLevel;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Application.UnitTests.Content;

public sealed class ContentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string AnyVersion = ContentVersion.Encode(new byte[8]);

    private readonly FakeLevelRepository _levels = new();
    private readonly FakeLessonRepository _lessons = new();
    private readonly FakeLearnerActivityReader _learnerActivity = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task New_levels_and_lessons_go_to_the_end()
    {
        var createLevel = new CreateLevelCommandHandler(_levels, _unitOfWork, TimeProvider.System);
        var first = await createLevel.Handle(new CreateLevelCommand("One", null), default);
        var second = await createLevel.Handle(new CreateLevelCommand("Two", null), default);

        var createLesson = new CreateLessonCommandHandler(_levels, _lessons, _unitOfWork, TimeProvider.System);
        await createLesson.Handle(new CreateLessonCommand(second, "A"), default);
        await createLesson.Handle(new CreateLessonCommand(second, "B"), default);

        Assert.Equal(1, _levels.Levels.Single(level => level.Id == first).SortOrder);
        Assert.Equal(2, _levels.Levels.Single(level => level.Id == second).SortOrder);
        Assert.Equal([1, 2], _lessons.Lessons.Select(lesson => lesson.SortOrder));
    }

    [Fact]
    public async Task Creating_a_lesson_in_a_missing_level_is_not_found()
    {
        var handler = new CreateLessonCommandHandler(_levels, _lessons, _unitOfWork, TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateLessonCommand(Guid.NewGuid(), "A"), default));
    }

    [Fact]
    public async Task A_lesson_that_children_started_is_not_unpublished()
    {
        var level = AddLevel(1);
        var lesson = AddLesson(level, 1);
        lesson.Publish(level, Now);
        _learnerActivity.StartedLessons.Add(lesson.Id);
        var handler = new UnpublishLessonCommandHandler(_levels, _lessons, _learnerActivity, _unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new UnpublishLessonCommand(lesson.Id, AnyVersion), default));
        Assert.Equal(ContentStatus.Published, lesson.Status);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Fact]
    public async Task A_level_is_unpublished_only_while_no_child_started_it()
    {
        var level = AddLevel(1);
        level.Publish(Now);
        var handler = new UnpublishLevelCommandHandler(_levels, _learnerActivity, _unitOfWork);

        _learnerActivity.StartedLevels.Add(level.Id);
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new UnpublishLevelCommand(level.Id, AnyVersion), default));

        _learnerActivity.StartedLevels.Clear();
        await handler.Handle(new UnpublishLevelCommand(level.Id, AnyVersion), default);
        Assert.Equal(ContentStatus.Draft, level.Status);
    }

    [Fact]
    public async Task Reordering_levels_swaps_positions_in_one_transaction()
    {
        var first = AddLevel(1);
        var second = AddLevel(2);
        var third = AddLevel(3);
        var handler = new ReorderLevelsCommandHandler(_levels, _unitOfWork);

        await handler.Handle(new ReorderLevelsCommand([third.Id, first.Id, second.Id]), default);

        Assert.Equal([2, 3, 1], new[] { first, second, third }.Select(level => level.SortOrder));
        Assert.Equal(1, _unitOfWork.Transactions);
        // Through positions past the highest one first, so the unique index never sees two equal positions.
        Assert.Equal(2, _unitOfWork.Saves);
    }

    [Fact]
    public async Task Reordering_with_a_missing_or_unknown_item_is_a_conflict()
    {
        var first = AddLevel(1);
        AddLevel(2);
        var handler = new ReorderLevelsCommandHandler(_levels, _unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ReorderLevelsCommand([first.Id]), default));
        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new ReorderLevelsCommand([first.Id, Guid.NewGuid()]), default));
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Fact]
    public async Task Archived_lessons_are_left_out_of_the_order_and_an_archived_level_cannot_be_reordered()
    {
        var level = AddLevel(1);
        var kept = AddLesson(level, 1);
        var archived = AddLesson(level, 2);
        var last = AddLesson(level, 3);
        archived.Archive(Now);
        var handler = new ReorderLessonsCommandHandler(_levels, _lessons, _unitOfWork);

        await handler.Handle(new ReorderLessonsCommand(level.Id, [last.Id, kept.Id]), default);
        Assert.Equal(1, last.SortOrder);
        Assert.Equal(2, kept.SortOrder);

        level.Archive(Now);
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new ReorderLessonsCommand(level.Id, [kept.Id, last.Id]), default));
    }

    private Level AddLevel(int sortOrder)
    {
        var level = Level.Create(ContentTitle.From($"Level {sortOrder}"), null, sortOrder, Now);
        _levels.Add(level);
        return level;
    }

    private Lesson AddLesson(Level level, int sortOrder)
    {
        var lesson = Lesson.Create(level, ContentTitle.From($"Lesson {sortOrder}"), sortOrder, Now);
        _lessons.Add(lesson);
        return lesson;
    }
}
