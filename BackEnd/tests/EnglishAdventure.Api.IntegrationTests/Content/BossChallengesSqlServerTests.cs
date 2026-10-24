using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.8 boss challenges (owner answers 2026-10-24) through the real API on SQL Server: one boss per level after its
/// lessons, activities that point to the lessons' words, the boss picture, and a level published only with its boss.
/// </summary>
public sealed class BossChallengesSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";
    private const string Picture = "https://cdn.example.test/picture.png";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public BossChallengesSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_level_has_one_boss_after_its_lessons()
    {
        var level = await NewLevelWithLessonAsync();

        var created = await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = "King Lion", imageUrl = Picture });
        var boss = await ReadAsync(created);
        var second = await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = "Queen Lion" });
        var lesson = await ReadAsync(await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/lessons", new { title = "Farm" }));
        var details = await ReadAsync(await level.Admin.GetAsync($"{LevelsRoute}/{level.Id}"));
        // The boss is not in the lessons' order.
        var reorder = await level.Admin.PutAsync(
            $"{LevelsRoute}/{level.Id}/lessons/order",
            new { lessonIds = new[] { lesson.GetProperty("id").GetString(), level.LessonId } });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Boss", boss.GetProperty("kind").GetString());
        Assert.Equal(Picture, boss.GetProperty("bossImageUrl").GetString());
        Assert.Equal("boss-exists", await CodeAsync(second));
        Assert.Equal(2, lesson.GetProperty("sortOrder").GetInt32());
        Assert.Equal(["Pets", "Farm", "King Lion"], Titles(details.GetProperty("lessons")));
        Assert.Equal("Draft", details.GetProperty("level").GetProperty("bossStatus").GetString());
        Assert.Equal(2, details.GetProperty("level").GetProperty("lessons").GetProperty("draft").GetInt32());
        Assert.Equal(HttpStatusCode.OK, reorder.StatusCode);
    }

    [SqlServerFact]
    public async Task A_boss_uses_the_words_of_the_level_lessons_and_has_none_of_its_own()
    {
        var level = await NewLevelWithLessonAsync();
        var boss = await CreateBossAsync(level, Picture);
        var route = $"{LessonsRoute}/{boss.GetProperty("id").GetString()}";

        var pool = await ReadAsync(await level.Admin.GetAsync($"{route}/words"));
        var ownWord = await level.Admin.PostAsync($"{route}/words", new { english = "lion", meaning = "sư tử", imageUrl = Picture, version = Version(boss) });
        var matching = await level.Admin.PostAsync(
            $"{route}/activities/matching",
            new { mode = "WordToPicture", wordIds = level.WordIds, version = Version(boss) });
        var removeUsed = await level.Admin.DeleteAsync(
            $"{level.LessonRoute}/words/{level.WordIds[0]}?version={Uri.EscapeDataString(level.LessonVersion)}");

        Assert.Equal(0, pool.GetProperty("maxWords").GetInt32());
        Assert.Equal(level.WordIds, QuizTestData.WordIds(pool));
        Assert.Equal("boss-no-word-bank", await CodeAsync(ownWord));
        Assert.Equal(HttpStatusCode.Created, matching.StatusCode);
        Assert.Equal("word-in-use", await CodeAsync(removeUsed));
    }

    [SqlServerFact]
    public async Task A_boss_is_published_with_its_picture_and_a_level_only_with_its_published_boss()
    {
        var level = await NewLevelWithLessonAsync();
        var boss = await CreateBossAsync(level, picture: null);
        var route = $"{LessonsRoute}/{boss.GetProperty("id").GetString()}";
        var activities = await ReadAsync(await level.Admin.PostAsync(
            $"{route}/activities/matching",
            new { mode = "WordToMeaning", wordIds = level.WordIds, version = Version(boss) }));

        var noPicture = await level.Admin.PostAsync($"{route}/publish", new { version = activities.GetProperty("lessonVersion").GetString() });
        boss = await ReadAsync(await level.Admin.PutAsync(
            $"{route}/boss",
            new { title = "King Lion", imageUrl = Picture, version = activities.GetProperty("lessonVersion").GetString() }));
        var levelVersion = Version((await ReadAsync(await level.Admin.GetAsync($"{LevelsRoute}/{level.Id}"))).GetProperty("level"));
        var levelWithoutBoss = await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/publish", new { version = levelVersion });
        boss = await ReadAsync(await level.Admin.PostAsync($"{route}/publish", new { version = Version(boss) }));
        var publishedLevel = await ReadAsync(await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/publish", new { version = levelVersion }));
        var unpublishBoss = await level.Admin.PostAsync($"{route}/unpublish", new { version = Version(boss) });
        var archiveBoss = await level.Admin.PostAsync($"{route}/archive", new { version = Version(boss) });

        Assert.Equal("lesson-not-ready", await CodeAsync(noPicture));
        Assert.Equal("level-needs-boss", await CodeAsync(levelWithoutBoss));
        Assert.Equal("Published", boss.GetProperty("status").GetString());
        Assert.Equal("Published", publishedLevel.GetProperty("level").GetProperty("status").GetString());
        Assert.Equal("Published", publishedLevel.GetProperty("level").GetProperty("bossStatus").GetString());
        Assert.Equal("boss-required", await CodeAsync(unpublishBoss));
        Assert.Equal("boss-required", await CodeAsync(archiveBoss));
    }

    [SqlServerFact]
    public async Task Bosses_need_content_manage_and_a_valid_picture()
    {
        var level = await NewLevelWithLessonAsync();
        await _fixture.GrantPermissionsAsync("BossReader", Permissions.Content.ReadDrafts);
        var reader = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: "BossReader"));

        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = "King Lion" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = "King Lion", imageUrl = "http://cdn.example.test/lion.png" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = " " })).StatusCode);
    }

    private sealed record TestLevel(
        AdminApiClient Admin,
        string Id,
        string LessonId,
        string LessonRoute,
        string LessonVersion,
        string[] WordIds);

    /// <summary>A ContentAdmin with a draft level and one draft lesson "Pets" of two words with pictures.</summary>
    private async Task<TestLevel> NewLevelWithLessonAsync()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var admin = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));
        var level = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Animals" }));
        var levelId = level.GetProperty("level").GetProperty("id").GetString()!;
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        var lessonId = lesson.GetProperty("id").GetString()!;
        var route = $"{LessonsRoute}/{lessonId}";

        var version = Version(lesson);
        JsonElement bank = default;
        foreach (var english in new[] { "cat", "dog" })
        {
            bank = await ReadAsync(await admin.PostAsync($"{route}/words", new { english, meaning = "nghĩa", imageUrl = Picture, version }));
            version = bank.GetProperty("lessonVersion").GetString()!;
        }

        return new TestLevel(admin, levelId, lessonId, route, version, [.. QuizTestData.WordIds(bank)]);
    }

    private static async Task<JsonElement> CreateBossAsync(TestLevel level, string? picture)
    {
        var response = await level.Admin.PostAsync($"{LevelsRoute}/{level.Id}/boss", new { title = "King Lion", imageUrl = picture });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync(response);
    }

    private static string Version(JsonElement item) => item.GetProperty("version").GetString()!;

    private static IEnumerable<string?> Titles(JsonElement items) =>
        items.EnumerateArray().Select(item => item.GetProperty("title").GetString());

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
