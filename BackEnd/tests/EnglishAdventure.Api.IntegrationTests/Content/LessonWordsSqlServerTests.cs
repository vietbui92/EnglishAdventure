using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.6 word bank (PD-011, PD-012) through the real API on SQL Server: words in order, pictures before publishing,
/// the structure lock of published lessons, the lesson version on every change, and the rule codes.
/// </summary>
public sealed class LessonWordsSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";
    private const string CatPicture = "https://cdn.example.test/cat.png";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public LessonWordsSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task Words_are_added_in_order_and_change_the_lesson_version()
    {
        var (admin, route, version) = await NewLessonAsync();

        var first = await admin.PostAsync($"{route}/words", Word("  cat ", " con mèo ", CatPicture, version));
        var bank = await ReadAsync(first);
        var second = await ReadAsync(await admin.PostAsync($"{route}/words", Word("dog", "con chó", null, LessonVersion(bank))));
        var stale = await admin.PostAsync($"{route}/words", Word("fish", "con cá", null, LessonVersion(bank)));
        var read = await ReadAsync(await admin.GetAsync($"{route}/words"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.NotEqual(version, LessonVersion(bank));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(12, read.GetProperty("maxWords").GetInt32());
        Assert.Equal("Draft", read.GetProperty("lessonStatus").GetString());
        Assert.Equal(LessonVersion(second), LessonVersion(read));
        var words = read.GetProperty("words").EnumerateArray().ToList();
        Assert.Equal(["cat", "dog"], words.Select(word => word.GetProperty("english").GetString()));
        Assert.Equal("con mèo", words[0].GetProperty("meaning").GetString());
        Assert.Equal(CatPicture, words[0].GetProperty("imageUrl").GetString());
        Assert.Equal(JsonValueKind.Null, words[1].GetProperty("imageUrl").ValueKind);
    }

    [SqlServerFact]
    public async Task Duplicate_words_and_bad_pictures_are_refused()
    {
        var (admin, route, version) = await NewLessonAsync();
        var bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("Cat", "con mèo", null, version)));

        var duplicate = await admin.PostAsync($"{route}/words", Word("cat", "mèo", null, LessonVersion(bank)));
        var http = await admin.PostAsync($"{route}/words", Word("dog", "con chó", "http://cdn.example.test/dog.png", LessonVersion(bank)));
        var blank = await admin.PostAsync($"{route}/words", Word(" ", "con chó", null, LessonVersion(bank)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicate.StatusCode);
        Assert.Equal("word-duplicate", await CodeAsync(duplicate));
        Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
    }

    [SqlServerFact]
    public async Task A_lesson_is_published_only_when_every_word_has_a_picture()
    {
        var (admin, route, version) = await NewLessonAsync();

        var empty = await admin.PostAsync($"{route}/publish", new { version });
        var bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("cat", "con mèo", null, version)));
        var noPicture = await admin.PostAsync($"{route}/publish", new { version = LessonVersion(bank) });
        var wordId = FirstWordId(bank);
        bank = await ReadAsync(await admin.PutAsync($"{route}/words/{wordId}", Word("cat", "con mèo", CatPicture, LessonVersion(bank))));
        bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("dog", "con chó", CatPicture, LessonVersion(bank))));
        var (_, ready) = await QuizTestData.AddQuizAsync(admin, route, LessonVersion(bank), wordId, QuizTestData.WordIds(bank)[1]);
        var published = await admin.PostAsync($"{route}/publish", new { version = ready });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        Assert.Equal("lesson-not-ready", await CodeAsync(empty));
        Assert.Equal("lesson-not-ready", await CodeAsync(noPicture));
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal("Published", (await ReadAsync(published)).GetProperty("status").GetString());
    }

    [SqlServerFact]
    public async Task A_published_lesson_corrects_words_but_keeps_its_structure()
    {
        var (admin, route, version) = await NewLessonAsync();
        var bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("cat", "con mèo", CatPicture, version)));
        bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("dog", "con chó", CatPicture, LessonVersion(bank))));
        var wordId = FirstWordId(bank);
        var (_, ready) = await QuizTestData.AddQuizAsync(admin, route, LessonVersion(bank), wordId, QuizTestData.WordIds(bank)[1]);
        var lesson = await ReadAsync(await admin.PostAsync($"{route}/publish", new { version = ready }));
        var current = lesson.GetProperty("version").GetString()!;

        var corrected = await ReadAsync(await admin.PutAsync(
            $"{route}/words/{wordId}",
            Word("cats", "những con mèo", "https://cdn.example.test/cats.png", current)));
        current = LessonVersion(corrected);
        var add = await admin.PostAsync($"{route}/words", Word("fish", "con cá", CatPicture, current));
        var remove = await admin.DeleteAsync($"{route}/words/{wordId}?version={Uri.EscapeDataString(current)}");
        var dropPicture = await admin.PutAsync($"{route}/words/{wordId}", Word("cats", "mèo", null, current));

        Assert.Equal("cats", corrected.GetProperty("words")[0].GetProperty("english").GetString());
        Assert.Equal("lesson-structure-locked", await CodeAsync(add));
        Assert.Equal("lesson-structure-locked", await CodeAsync(remove));
        Assert.Equal("lesson-not-ready", await CodeAsync(dropPicture));
    }

    [SqlServerFact]
    public async Task Words_of_a_draft_are_removed_and_reordered()
    {
        var (admin, route, version) = await NewLessonAsync();
        var bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("cat", "con mèo", null, version)));
        bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("dog", "con chó", null, LessonVersion(bank))));
        bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("fish", "con cá", null, LessonVersion(bank))));
        var ids = bank.GetProperty("words").EnumerateArray().Select(word => word.GetProperty("id").GetString()!).ToList();

        bank = await ReadAsync(await admin.DeleteAsync($"{route}/words/{ids[1]}?version={Uri.EscapeDataString(LessonVersion(bank))}"));
        var reordered = await admin.PutAsync($"{route}/words/order", new { wordIds = new[] { ids[2], ids[0] }, version = LessonVersion(bank) });
        bank = await ReadAsync(reordered);
        var mismatch = await admin.PutAsync($"{route}/words/order", new { wordIds = new[] { ids[0] }, version = LessonVersion(bank) });
        var otherLessonWord = await admin.PutAsync(
            $"{LessonsRoute}/{Guid.NewGuid()}/words/{ids[0]}",
            Word("cat", "con mèo", null, LessonVersion(bank)));

        Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);
        Assert.Equal(["fish", "cat"], bank.GetProperty("words").EnumerateArray().Select(word => word.GetProperty("english").GetString()));
        Assert.Equal([1, 2], bank.GetProperty("words").EnumerateArray().Select(word => word.GetProperty("sortOrder").GetInt32()));
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherLessonWord.StatusCode);
    }

    [SqlServerFact]
    public async Task Learn_cards_are_chosen_in_a_draft_and_a_lesson_needs_one_to_be_published()
    {
        var (admin, route, version) = await NewLessonAsync();
        var bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("cat", "con mèo", CatPicture, version)));
        bank = await ReadAsync(await admin.PostAsync($"{route}/words", Word("dog", "con chó", CatPicture, LessonVersion(bank))));
        var wordId = FirstWordId(bank);
        var dogId = QuizTestData.WordIds(bank)[1];
        Assert.True(bank.GetProperty("words")[0].GetProperty("showInLearnStep").GetBoolean());
        var (_, current) = await QuizTestData.AddQuizAsync(admin, route, LessonVersion(bank), wordId, dogId);

        bank = await ReadAsync(await admin.PutAsync($"{route}/words/{wordId}/learn-card", new { show = false, version = current }));
        bank = await ReadAsync(await admin.PutAsync($"{route}/words/{dogId}/learn-card", new { show = false, version = LessonVersion(bank) }));
        var noCard = await admin.PostAsync($"{route}/publish", new { version = LessonVersion(bank) });
        bank = await ReadAsync(await admin.PutAsync($"{route}/words/{wordId}/learn-card", new { show = true, version = LessonVersion(bank) }));
        var lesson = await ReadAsync(await admin.PostAsync($"{route}/publish", new { version = LessonVersion(bank) }));
        var locked = await admin.PutAsync(
            $"{route}/words/{wordId}/learn-card",
            new { show = false, version = lesson.GetProperty("version").GetString() });

        Assert.True(bank.GetProperty("words")[0].GetProperty("showInLearnStep").GetBoolean());
        Assert.Equal("lesson-not-ready", await CodeAsync(noCard));
        Assert.Equal("Published", lesson.GetProperty("status").GetString());
        Assert.Equal("lesson-structure-locked", await CodeAsync(locked));
    }

    [SqlServerFact]
    public async Task Changing_words_needs_content_manage()
    {
        var (admin, route, version) = await NewLessonAsync();
        await _fixture.GrantPermissionsAsync("WordReader", Permissions.Content.ReadDrafts);
        var reader = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: "WordReader"));
        var parent = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent));

        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{route}/words")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync($"{route}/words", Word("cat", "con mèo", null, version))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync($"{route}/words")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync($"{route}/words")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsync($"{route}/words", Word("cat", "con mèo", null, version))).StatusCode);
    }

    /// <summary>A ContentAdmin with a new draft lesson: the client, the lesson route and the lesson's version.</summary>
    private async Task<(AdminApiClient Admin, string Route, string Version)> NewLessonAsync()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var admin = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));
        var level = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Animals" }));
        var levelId = level.GetProperty("level").GetProperty("id").GetString();
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        return (admin, $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}", lesson.GetProperty("version").GetString()!);
    }

    private static object Word(string english, string meaning, string? imageUrl, string version) =>
        new { english, meaning, imageUrl, version };

    private static string LessonVersion(JsonElement bank) => bank.GetProperty("lessonVersion").GetString()!;

    private static string FirstWordId(JsonElement bank) => bank.GetProperty("words")[0].GetProperty("id").GetString()!;

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
