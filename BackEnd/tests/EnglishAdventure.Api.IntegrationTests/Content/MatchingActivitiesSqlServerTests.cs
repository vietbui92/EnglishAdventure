using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.3 matching activities (owner answers 2026-10-24) through the real API on SQL Server: 2–6 words of the lesson
/// matched with pictures or meanings, 400 for a badly shaped matching, the structure lock and publishing.
/// </summary>
public sealed class MatchingActivitiesSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public MatchingActivitiesSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_matching_is_added_changed_and_listed_with_the_quizzes()
    {
        var (admin, route, version, words) = await NewLessonWithWordsAsync(4);

        var added = await admin.PostAsync($"{route}/activities/matching", Matching("WordToPicture", [words[2], words[0], words[1]], version));
        var activities = await ReadAsync(added);
        var matchingId = activities.GetProperty("activities")[0].GetProperty("id").GetString();
        activities = await ReadAsync(await admin.PutAsync(
            $"{route}/activities/{matchingId}/matching",
            Matching("WordToMeaning", [words[3], words[2]], LessonVersion(activities))));
        activities = await ReadAsync(await admin.PostAsync($"{route}/activities/quiz", new { version = LessonVersion(activities) }));

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var list = activities.GetProperty("activities").EnumerateArray().ToList();
        Assert.Equal(["Matching", "Quiz"], list.Select(activity => activity.GetProperty("type").GetString()));
        Assert.Equal("WordToMeaning", list[0].GetProperty("matchingMode").GetString());
        Assert.Equal([words[3], words[2]], list[0].GetProperty("pairWordIds").EnumerateArray().Select(id => id.GetString()));
        Assert.Equal(JsonValueKind.Null, list[1].GetProperty("matchingMode").ValueKind);
        Assert.Equal(0, list[1].GetProperty("pairWordIds").GetArrayLength());
    }

    [SqlServerFact]
    public async Task Badly_shaped_matchings_are_refused()
    {
        var (admin, route, version, words) = await NewLessonWithWordsAsync(7);
        var add = $"{route}/activities/matching";

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(add, Matching("WordToPicture", [words[0]], version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(add, Matching("WordToPicture", [.. words], version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(add, Matching("WordToPicture", [words[0], words[0]], version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(add, Matching("Memory", [words[0], words[1]], version))).StatusCode);

        var stranger = await admin.PostAsync(add, Matching("WordToPicture", [words[0], Guid.NewGuid().ToString()], version));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stranger.StatusCode);
        Assert.Equal("matching-invalid", await CodeAsync(stranger));
    }

    [SqlServerFact]
    public async Task A_lesson_with_a_matching_is_published_and_then_locks_it()
    {
        var (admin, route, version, words) = await NewLessonWithWordsAsync(2);
        var activities = await ReadAsync(await admin.PostAsync($"{route}/activities/matching", Matching("WordToPicture", [words[0], words[1]], version)));
        var matchingId = activities.GetProperty("activities")[0].GetProperty("id").GetString();

        var published = await ReadAsync(await admin.PostAsync($"{route}/publish", new { version = LessonVersion(activities) }));
        var current = published.GetProperty("version").GetString()!;
        var change = await admin.PutAsync($"{route}/activities/{matchingId}/matching", Matching("WordToMeaning", [words[0], words[1]], current));
        var removeWord = await admin.DeleteAsync($"{route}/words/{words[0]}?version={Uri.EscapeDataString(current)}");

        Assert.Equal("Published", published.GetProperty("status").GetString());
        Assert.Equal("lesson-structure-locked", await CodeAsync(change));
        Assert.Equal("lesson-structure-locked", await CodeAsync(removeWord));
    }

    [SqlServerFact]
    public async Task A_word_in_a_matching_cannot_be_removed_and_quiz_routes_refuse_a_matching()
    {
        var (admin, route, version, words) = await NewLessonWithWordsAsync(3);
        var activities = await ReadAsync(await admin.PostAsync($"{route}/activities/matching", Matching("WordToPicture", [words[0], words[1]], version)));
        var matchingId = activities.GetProperty("activities")[0].GetProperty("id").GetString();
        version = LessonVersion(activities);

        var removeUsed = await admin.DeleteAsync($"{route}/words/{words[0]}?version={Uri.EscapeDataString(version)}");
        var question = await admin.PostAsync(
            $"{route}/activities/{matchingId}/questions",
            new { form = "PictureToWord", correctWordId = words[0], wrongWordIds = new[] { words[1] }, sentence = (string?)null, version });
        var removeUnused = await admin.DeleteAsync($"{route}/words/{words[2]}?version={Uri.EscapeDataString(version)}");

        Assert.Equal("word-in-use", await CodeAsync(removeUsed));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, question.StatusCode);
        Assert.Equal(HttpStatusCode.OK, removeUnused.StatusCode);
    }

    [SqlServerFact]
    public async Task Changing_a_matching_needs_content_manage()
    {
        var (_, route, version, words) = await NewLessonWithWordsAsync(2);
        await _fixture.GrantPermissionsAsync("MatchingReader", Permissions.Content.ReadDrafts);
        var reader = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: "MatchingReader"));

        var add = await reader.PostAsync($"{route}/activities/matching", Matching("WordToPicture", [words[0], words[1]], version));

        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
    }

    /// <summary>A ContentAdmin with a new draft lesson of <paramref name="count"/> words with pictures.</summary>
    private async Task<(AdminApiClient Admin, string Route, string Version, List<string> Words)> NewLessonWithWordsAsync(int count)
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var admin = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));
        var level = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Animals" }));
        var levelId = level.GetProperty("level").GetProperty("id").GetString();
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        var route = $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}";

        var version = lesson.GetProperty("version").GetString()!;
        JsonElement bank = default;
        for (var index = 0; index < count; index++)
        {
            bank = await ReadAsync(await admin.PostAsync(
                $"{route}/words",
                new { english = $"word {index}", meaning = "nghĩa", imageUrl = "https://cdn.example.test/picture.png", version }));
            version = LessonVersion(bank);
        }

        return (admin, route, version, QuizTestData.WordIds(bank));
    }

    private static object Matching(string mode, string[] wordIds, string version) => new { mode, wordIds, version };

    private static string LessonVersion(JsonElement element) => element.GetProperty("lessonVersion").GetString()!;

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
