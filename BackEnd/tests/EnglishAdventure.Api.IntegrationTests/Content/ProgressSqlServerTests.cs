using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S4.2 completion, best stars and unlocking through the real API on SQL Server (owner decisions 2026-10-24): a lesson
/// is completed with one star or more, replays only raise the best stars, the boss opens when every lesson has a star,
/// and a lesson that is not open can be neither read nor started.
/// </summary>
public sealed class ProgressSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string PlayRoute = "/api/v1/play";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public ProgressSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_new_child_starts_with_the_first_lesson_and_the_boss_is_locked()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        var map = await ReadAsync(await child.GetAsync($"{PlayRoute}/map"));
        var read = await child.GetAsync($"{PlayRoute}/lessons/{content.BossId}");
        var start = await child.PostAsync($"{PlayRoute}/lessons/{content.BossId}/attempts");

        Assert.Equal(1, map.GetProperty("currentLevel").GetInt32());
        var level = Level(map, content.LevelId);
        Assert.True(level.GetProperty("unlocked").GetBoolean());
        Assert.False(level.GetProperty("beaten").GetBoolean());
        Assert.Equal("Available", Lesson(level, content.LessonId).GetProperty("state").GetString());
        Assert.Equal(0, Lesson(level, content.LessonId).GetProperty("bestStars").GetInt32());
        Assert.Equal("Locked", level.GetProperty("boss").GetProperty("state").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, read.StatusCode);
        Assert.Equal("lesson-locked", await CodeAsync(read));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, start.StatusCode);
        Assert.Equal("lesson-locked", await CodeAsync(start));
    }

    [SqlServerFact]
    public async Task Completing_the_lessons_opens_the_boss_and_defeating_it_beats_the_level()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        // 4 of 5 at once: two stars.
        await PlayTestData.PlayPetsAsync(child, content.LessonId, wrongAnswers: 1);
        var level = Level(await ReadAsync(await child.GetAsync($"{PlayRoute}/map")), content.LevelId);
        Assert.Equal("Completed", Lesson(level, content.LessonId).GetProperty("state").GetString());
        Assert.Equal(2, Lesson(level, content.LessonId).GetProperty("bestStars").GetInt32());
        Assert.Equal("Available", level.GetProperty("boss").GetProperty("state").GetString());

        await DefeatBossAsync(child, content.BossId);

        level = Level(await ReadAsync(await child.GetAsync($"{PlayRoute}/map")), content.LevelId);
        Assert.Equal("Completed", level.GetProperty("boss").GetProperty("state").GetString());
        Assert.Equal(3, level.GetProperty("boss").GetProperty("bestStars").GetInt32());
        Assert.True(level.GetProperty("beaten").GetBoolean());
    }

    [SqlServerFact]
    public async Task A_worse_replay_never_lowers_the_best_stars()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        await PlayTestData.PlayPetsAsync(child, content.LessonId);
        // 2 of 5: no star this time.
        var replay = await PlayTestData.PlayPetsAsync(child, content.LessonId, wrongAnswers: 3);

        Assert.Equal(0, replay.GetProperty("result").GetProperty("stars").GetInt32());
        var lesson = Lesson(Level(await ReadAsync(await child.GetAsync($"{PlayRoute}/map")), content.LevelId), content.LessonId);
        Assert.Equal("Completed", lesson.GetProperty("state").GetString());
        Assert.Equal(3, lesson.GetProperty("bestStars").GetInt32());
        Assert.Equal(1, await CompletionsAsync(content.LessonId));
    }

    [SqlServerFact]
    public async Task Each_child_has_their_own_progress()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var first = await AdminApiClient.StartChildSessionAsync(_fixture);
        var second = await AdminApiClient.StartChildSessionAsync(_fixture);

        await PlayTestData.PlayPetsAsync(first, content.LessonId);

        var level = Level(await ReadAsync(await second.GetAsync($"{PlayRoute}/map")), content.LevelId);
        Assert.Equal("Available", Lesson(level, content.LessonId).GetProperty("state").GetString());
        Assert.Equal("Locked", level.GetProperty("boss").GetProperty("state").GetString());
    }

    [SqlServerFact]
    public async Task An_unfinished_boss_stays_playable_when_a_new_lesson_closes_it()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        await PlayTestData.PlayPetsAsync(child, content.LessonId);
        await DefeatBossAsync(child, content.BossId, holdLast: true);

        // The Admin adds a lesson to the level: the boss closes until it has a star.
        await PlayTestData.PublishFarmAsync(content);
        var level = Level(await ReadAsync(await child.GetAsync($"{PlayRoute}/map")), content.LevelId);
        Assert.Equal("Available", Lesson(level, content.DraftLessonId).GetProperty("state").GetString());
        Assert.Equal("Locked", level.GetProperty("boss").GetProperty("state").GetString());

        // The fight already started can still be finished…
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync($"{PlayRoute}/lessons/{content.BossId}")).StatusCode);
        var (attemptId, leftId, rightId) = _lastPair!.Value;
        var last = await child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId });
        Assert.Equal("Finished", (await ReadAsync(last)).GetProperty("attempt").GetProperty("status").GetString());

        // …but not started again, and the defeated boss keeps its stars.
        var again = await child.PostAsync($"{PlayRoute}/lessons/{content.BossId}/attempts");
        Assert.Equal("lesson-locked", await CodeAsync(again));
        level = Level(await ReadAsync(await child.GetAsync($"{PlayRoute}/map")), content.LevelId);
        Assert.Equal(3, level.GetProperty("boss").GetProperty("bestStars").GetInt32());
        Assert.True(level.GetProperty("beaten").GetBoolean());
    }

    [SqlServerFact]
    public async Task The_last_answer_sent_twice_at_once_completes_the_lesson_once()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        await PlayTestData.PlayPetsAsync(child, content.LessonId);
        await DefeatBossAsync(child, content.BossId, holdLast: true);

        // The boss's last pair, sent twice at the same moment.
        var (attemptId, leftId, rightId) = _lastPair!.Value;
        var responses = await Task.WhenAll(
            child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId }),
            child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId }));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal(1, await CompletionsAsync(content.BossId));
    }

    private (string AttemptId, string LeftId, string RightId)? _lastPair;

    /// <summary>Plays the boss (a matching of cat, dog and fish with their meanings), every pair right at once.</summary>
    private async Task DefeatBossAsync(AdminApiClient child, string bossId, bool holdLast = false)
    {
        var attempt = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{bossId}/attempts"));
        var boss = await ReadAsync(await child.GetAsync($"{PlayRoute}/lessons/{bossId}"));
        var attemptId = attempt.GetProperty("attemptId").GetString()!;
        var matching = boss.GetProperty("activities")[0].GetProperty("matching");
        var pairs = new[] { ("cat", "con mèo"), ("dog", "con chó"), ("fish", "con cá") }
            .Select(pair => (
                Left: matching.GetProperty("left").EnumerateArray()
                    .Single(choice => choice.GetProperty("text").GetString() == pair.Item1).GetProperty("id").GetString()!,
                Right: matching.GetProperty("right").EnumerateArray()
                    .Single(choice => choice.GetProperty("text").GetString() == pair.Item2).GetProperty("id").GetString()!))
            .ToList();

        foreach (var (left, right) in holdLast ? pairs[..^1] : pairs)
        {
            var matched = await child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId = left, rightId = right });
            Assert.Equal(HttpStatusCode.OK, matched.StatusCode);
        }

        _lastPair = (attemptId, pairs[^1].Left, pairs[^1].Right);
    }

    private async Task<int> CompletionsAsync(string lessonId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>();
        return await dbContext.LessonCompletions.CountAsync(completion => completion.LessonId == Guid.Parse(lessonId));
    }

    private static JsonElement Level(JsonElement map, string levelId) =>
        map.GetProperty("levels").EnumerateArray().Single(level => level.GetProperty("id").GetString() == levelId);

    private static JsonElement Lesson(JsonElement level, string lessonId) =>
        level.GetProperty("lessons").EnumerateArray().Single(lesson => lesson.GetProperty("id").GetString() == lessonId);

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
