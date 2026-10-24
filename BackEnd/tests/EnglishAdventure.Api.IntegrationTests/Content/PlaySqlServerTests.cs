using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.4 learner lesson map through the real API on SQL Server: a child session sees only published levels, lessons and
/// bosses, and a lesson to play never gives its answers away.
/// </summary>
public sealed class PlaySqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";
    private const string PlayRoute = "/api/v1/play";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public PlaySqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task The_map_shows_only_published_levels_lessons_and_bosses()
    {
        var content = await PublishLevelAsync();
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        var map = await ReadAsync(await child.GetAsync($"{PlayRoute}/map"));

        var levels = map.GetProperty("levels").EnumerateArray().ToList();
        var level = levels.Single(candidate => candidate.GetProperty("id").GetString() == content.LevelId);
        Assert.DoesNotContain(levels, candidate => candidate.GetProperty("id").GetString() == content.DraftLevelId);
        Assert.Equal("Animal Island", level.GetProperty("title").GetString());
        Assert.Equal(
            [content.LessonId],
            level.GetProperty("lessons").EnumerateArray().Select(lesson => lesson.GetProperty("id").GetString()));
        Assert.Equal(content.BossId, level.GetProperty("boss").GetProperty("id").GetString());
        Assert.Equal("King Lion", level.GetProperty("boss").GetProperty("name").GetString());
    }

    [SqlServerFact]
    public async Task A_lesson_to_play_has_its_cards_and_games_but_never_its_answers()
    {
        var content = await PublishLevelAsync();
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        var response = await child.GetAsync($"{PlayRoute}/lessons/{content.LessonId}");
        var raw = await response.Content.ReadAsStringAsync();
        var lesson = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("correct", raw, StringComparison.OrdinalIgnoreCase);
        // No word identity is sent, so nothing can be linked back to a learn card or a word.
        foreach (var wordId in content.WordIds)
        {
            Assert.DoesNotContain(wordId, raw, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal("Regular", lesson.GetProperty("kind").GetString());
        Assert.Equal(
            ["cat", "dog", "fish"],
            lesson.GetProperty("learnCards").EnumerateArray().Select(card => card.GetProperty("english").GetString()));

        var activities = lesson.GetProperty("activities").EnumerateArray().ToList();
        var questions = activities[0].GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(["PictureToWord", "WordToPicture", "FillInTheBlank"], questions.Select(question => question.GetProperty("form").GetString()));

        // Picture → word: the answer's picture, options as words only.
        Assert.Equal(Picture("cat"), questions[0].GetProperty("promptImageUrl").GetString());
        Assert.Equal(["cat", "dog", "fish"], Texts(questions[0]).Order());
        Assert.All(Options(questions[0]), option => Assert.Equal(JsonValueKind.Null, option.GetProperty("imageUrl").ValueKind));
        // Word → picture: the English word, options as pictures only.
        Assert.Equal("dog", questions[1].GetProperty("promptText").GetString());
        Assert.All(Options(questions[1]), option => Assert.Equal(JsonValueKind.Null, option.GetProperty("text").ValueKind));
        Assert.Equal([Picture("cat"), Picture("dog")], Options(questions[1]).Select(option => option.GetProperty("imageUrl").GetString()).Order());
        // Fill in: the sentence with its gap.
        Assert.Equal("A ___ swims.", questions[2].GetProperty("promptText").GetString());

        // Matching: the two sides use different identities.
        var matching = activities[1].GetProperty("matching");
        var left = matching.GetProperty("left").EnumerateArray().Select(choice => choice.GetProperty("id").GetString()).ToList();
        var right = matching.GetProperty("right").EnumerateArray().Select(choice => choice.GetProperty("id").GetString()).ToList();
        Assert.Equal("WordToPicture", matching.GetProperty("mode").GetString());
        Assert.Equal(2, left.Count);
        Assert.Empty(left.Intersect(right));
        Assert.Equal(
            [Picture("cat"), Picture("dog")],
            matching.GetProperty("right").EnumerateArray().Select(choice => choice.GetProperty("imageUrl").GetString()).Order());
    }

    [SqlServerFact]
    public async Task Options_come_in_a_new_order()
    {
        var content = await PublishLevelAsync();
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        // The stored order puts the correct option first; reading often must show other orders.
        var orders = new HashSet<string>();
        for (var read = 0; read < 20 && orders.Count < 2; read++)
        {
            var lesson = await ReadAsync(await child.GetAsync($"{PlayRoute}/lessons/{content.LessonId}"));
            orders.Add(string.Join(",", Texts(lesson.GetProperty("activities")[0].GetProperty("questions")[0])));
        }

        Assert.True(orders.Count > 1);
    }

    [SqlServerFact]
    public async Task A_boss_is_played_with_the_level_words_and_drafts_are_not_found()
    {
        var content = await PublishLevelAsync();
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        var boss = await ReadAsync(await child.GetAsync($"{PlayRoute}/lessons/{content.BossId}"));
        var draft = await child.GetAsync($"{PlayRoute}/lessons/{content.DraftLessonId}");
        var unknown = await child.GetAsync($"{PlayRoute}/lessons/{Guid.NewGuid()}");

        Assert.Equal("Boss", boss.GetProperty("kind").GetString());
        Assert.Equal(Picture("lion"), boss.GetProperty("bossImageUrl").GetString());
        Assert.Equal(0, boss.GetProperty("learnCards").GetArrayLength());
        var matching = boss.GetProperty("activities")[0].GetProperty("matching");
        Assert.Equal("WordToMeaning", matching.GetProperty("mode").GetString());
        Assert.Equal(
            new[] { "con mèo", "con chó", "con cá" }.Order(),
            matching.GetProperty("right").EnumerateArray().Select(choice => choice.GetProperty("text").GetString()!).Order());
        Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [SqlServerFact]
    public async Task Only_a_child_session_plays()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var parent = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent));
        var admin = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync($"{PlayRoute}/map")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync($"{PlayRoute}/map")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"{PlayRoute}/map")).StatusCode);
    }

    private sealed record PublishedContent(
        string LevelId,
        string LessonId,
        string BossId,
        string DraftLessonId,
        string DraftLevelId,
        List<string> WordIds);

    /// <summary>
    /// Builds and publishes a level: lesson "Pets" (cat, dog, fish) with a quiz of the three forms and a matching, a
    /// draft lesson, and the boss "King Lion" matching all three words with their meanings. Also a draft level.
    /// </summary>
    private async Task<PublishedContent> PublishLevelAsync()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var admin = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));
        var draftLevel = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Draft Land" }));
        var level = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Animal Island" }));
        var levelId = level.GetProperty("level").GetProperty("id").GetString()!;
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        var draftLesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Farm" }));
        var route = $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}";

        var version = lesson.GetProperty("version").GetString()!;
        JsonElement bank = default;
        foreach (var (english, meaning) in new[] { ("cat", "con mèo"), ("dog", "con chó"), ("fish", "con cá") })
        {
            bank = await ReadAsync(await admin.PostAsync($"{route}/words", new { english, meaning, imageUrl = Picture(english), version }));
            version = LessonVersion(bank);
        }

        var words = QuizTestData.WordIds(bank);
        var activities = await ReadAsync(await admin.PostAsync($"{route}/activities/quiz", new { version }));
        var quizRoute = $"{route}/activities/{activities.GetProperty("activities")[0].GetProperty("id").GetString()}/questions";
        activities = await ReadAsync(await admin.PostAsync(quizRoute, Question("PictureToWord", words[0], [words[1], words[2]], null, LessonVersion(activities))));
        activities = await ReadAsync(await admin.PostAsync(quizRoute, Question("WordToPicture", words[1], [words[0]], null, LessonVersion(activities))));
        activities = await ReadAsync(await admin.PostAsync(quizRoute, Question("FillInTheBlank", words[2], [words[0]], "A ___ swims.", LessonVersion(activities))));
        activities = await ReadAsync(await admin.PostAsync(
            $"{route}/activities/matching",
            new { mode = "WordToPicture", wordIds = new[] { words[0], words[1] }, version = LessonVersion(activities) }));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{route}/publish", new { version = LessonVersion(activities) })).StatusCode);

        var boss = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/boss", new { title = "King Lion", imageUrl = Picture("lion") }));
        var bossRoute = $"{LessonsRoute}/{boss.GetProperty("id").GetString()}";
        var bossActivities = await ReadAsync(await admin.PostAsync(
            $"{bossRoute}/activities/matching",
            new { mode = "WordToMeaning", wordIds = words, version = boss.GetProperty("version").GetString() }));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{bossRoute}/publish", new { version = LessonVersion(bossActivities) })).StatusCode);

        var levelVersion = (await ReadAsync(await admin.GetAsync($"{LevelsRoute}/{levelId}"))).GetProperty("level").GetProperty("version").GetString();
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{LevelsRoute}/{levelId}/publish", new { version = levelVersion })).StatusCode);

        return new PublishedContent(
            levelId,
            lesson.GetProperty("id").GetString()!,
            boss.GetProperty("id").GetString()!,
            draftLesson.GetProperty("id").GetString()!,
            draftLevel.GetProperty("level").GetProperty("id").GetString()!,
            words);
    }

    private static string Picture(string name) => $"https://cdn.example.test/{name}.png";

    private static object Question(string form, string correctWordId, string[] wrongWordIds, string? sentence, string version) =>
        new { form, correctWordId, wrongWordIds, sentence, version };

    private static string LessonVersion(JsonElement element) => element.GetProperty("lessonVersion").GetString()!;

    private static IEnumerable<JsonElement> Options(JsonElement question) => question.GetProperty("options").EnumerateArray();

    private static IEnumerable<string?> Texts(JsonElement question) =>
        Options(question).Select(option => option.GetProperty("text").GetString());

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
