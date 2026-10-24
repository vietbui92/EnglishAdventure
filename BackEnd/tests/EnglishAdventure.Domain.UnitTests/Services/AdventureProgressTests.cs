using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Services;

namespace EnglishAdventure.Domain.UnitTests.Services;

public sealed class AdventureProgressTests
{
    private readonly Guid _pets = Guid.NewGuid();
    private readonly Guid _farm = Guid.NewGuid();
    private readonly Guid _lion = Guid.NewGuid();
    private readonly Guid _fruit = Guid.NewGuid();
    private readonly Guid _apple = Guid.NewGuid();
    private readonly Guid _monkey = Guid.NewGuid();

    private IReadOnlyList<LevelPath> Map(params Guid[] firstLevelLessons) =>
    [
        new LevelPath(Guid.NewGuid(), firstLevelLessons.Length == 0 ? [_pets, _farm] : firstLevelLessons, _lion),
        new LevelPath(Guid.NewGuid(), [_fruit, _apple], _monkey),
    ];

    private static Dictionary<Guid, int> Stars(params (Guid Lesson, int Stars)[] best) =>
        best.ToDictionary(entry => entry.Lesson, entry => entry.Stars);

    private static LessonState State(AdventureProgress progress, Guid lessonId) => progress.Find(lessonId)!.State;

    [Fact]
    public void A_new_child_starts_with_the_first_lesson_of_the_first_level()
    {
        var progress = AdventureProgress.Compute(Map(), Stars());

        Assert.Equal(LessonState.Available, State(progress, _pets));
        Assert.Equal(LessonState.Locked, State(progress, _farm));
        Assert.Equal(LessonState.Locked, State(progress, _lion));
        Assert.Equal(LessonState.Locked, State(progress, _fruit));
        Assert.True(progress.Levels[0].Unlocked);
        Assert.False(progress.Levels[1].Unlocked);
        Assert.Equal(1, progress.CurrentLevel);
    }

    [Fact]
    public void A_star_opens_the_next_lesson_and_all_of_them_open_the_boss()
    {
        var one = AdventureProgress.Compute(Map(), Stars((_pets, 1)));
        var both = AdventureProgress.Compute(Map(), Stars((_pets, 1), (_farm, 3)));

        Assert.Equal(LessonState.Completed, State(one, _pets));
        Assert.Equal(1, one.Find(_pets)!.BestStars);
        Assert.Equal(LessonState.Available, State(one, _farm));
        Assert.Equal(LessonState.Locked, State(one, _lion));
        Assert.Equal(LessonState.Available, State(both, _lion));
        Assert.False(both.Levels[0].Beaten);
    }

    [Fact]
    public void Defeating_the_boss_opens_the_next_level()
    {
        var progress = AdventureProgress.Compute(Map(), Stars((_pets, 2), (_farm, 2), (_lion, 1)));

        Assert.Equal(LessonState.Completed, State(progress, _lion));
        Assert.True(progress.Levels[0].Beaten);
        Assert.True(progress.Levels[1].Unlocked);
        Assert.Equal(LessonState.Available, State(progress, _fruit));
        Assert.Equal(LessonState.Locked, State(progress, _apple));
        Assert.Equal(2, progress.CurrentLevel);
    }

    [Fact]
    public void A_new_lesson_put_before_completed_ones_keeps_them_open_and_closes_the_boss_again()
    {
        var newLesson = Guid.NewGuid();
        // The Admin added a lesson first, after the child had beaten the level.
        var progress = AdventureProgress.Compute(
            Map(newLesson, _pets, _farm),
            Stars((_pets, 3), (_farm, 2), (_lion, 2)));

        Assert.Equal(LessonState.Available, State(progress, newLesson));
        Assert.Equal(LessonState.Completed, State(progress, _pets));
        Assert.True(progress.Find(_pets)!.CanStart);
        Assert.Equal(LessonState.Locked, State(progress, _lion));
        Assert.Equal(2, progress.Find(_lion)!.BestStars);
        Assert.False(progress.Find(_lion)!.CanStart);
        // The boss was defeated, so the next level stays open.
        Assert.True(progress.Levels[1].Unlocked);
        Assert.Equal(LessonState.Available, State(progress, _fruit));
    }

    [Fact]
    public void Reordering_keeps_completed_lessons_and_opens_what_follows_a_completed_one()
    {
        // Farm was moved before Pets; the child had completed only Pets.
        var progress = AdventureProgress.Compute(Map(_farm, _pets), Stars((_pets, 1)));

        Assert.Equal(LessonState.Available, State(progress, _farm));
        Assert.Equal(LessonState.Completed, State(progress, _pets));
        Assert.Equal(LessonState.Locked, State(progress, _lion));
    }

    [Fact]
    public void Lessons_of_a_locked_level_are_locked_even_after_the_first()
    {
        var progress = AdventureProgress.Compute(Map(), Stars((_pets, 1), (_farm, 1)));

        Assert.Equal(LessonState.Locked, State(progress, _fruit));
        Assert.Equal(LessonState.Locked, State(progress, _apple));
        Assert.Equal(LessonState.Locked, State(progress, _monkey));
        Assert.Null(progress.Find(Guid.NewGuid()));
    }

    [Fact]
    public void A_level_without_a_boss_is_beaten_by_its_lessons()
    {
        IReadOnlyList<LevelPath> map =
        [
            new LevelPath(Guid.NewGuid(), [_pets], BossId: null),
            new LevelPath(Guid.NewGuid(), [_fruit], BossId: null),
        ];

        Assert.False(AdventureProgress.Compute(map, Stars()).Levels[1].Unlocked);
        Assert.True(AdventureProgress.Compute(map, Stars((_pets, 1))).Levels[1].Unlocked);
        Assert.Equal(0, AdventureProgress.Compute([], Stars()).CurrentLevel);
    }
}
