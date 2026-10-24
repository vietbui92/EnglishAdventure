using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.1 levels and lessons (PD-012) through the real API on SQL Server: order, publish, unpublish, final archiving,
/// optimistic concurrency and the <c>content.*</c> permissions. Levels are shared by every test of this class, so tests
/// look only at the levels they created, except where the whole order is the point.
/// </summary>
public sealed class ContentLevelsAndLessonsSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public ContentLevelsAndLessonsSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_content_admin_adds_levels_and_lessons_at_the_end()
    {
        var admin = await SignInAsync(SystemRoles.ContentAdmin);

        var first = await CreateLevelAsync(admin, "Animal Island", "  Cats and dogs ");
        var second = await CreateLevelAsync(admin, "Color Garden");
        var levelId = Id(second);
        var pets = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        var created = await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Farm" });
        var level = await ReadAsync(await admin.GetAsync($"{LevelsRoute}/{levelId}"));

        Assert.Equal("Draft", first.GetProperty("level").GetProperty("status").GetString());
        Assert.Equal("Cats and dogs", first.GetProperty("level").GetProperty("description").GetString());
        Assert.True(
            second.GetProperty("level").GetProperty("sortOrder").GetInt32()
            > first.GetProperty("level").GetProperty("sortOrder").GetInt32());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(1, pets.GetProperty("sortOrder").GetInt32());
        Assert.Equal(["Pets", "Farm"], Titles(level.GetProperty("lessons")));
        Assert.Equal(2, level.GetProperty("level").GetProperty("lessons").GetProperty("draft").GetInt32());
        Assert.False(string.IsNullOrEmpty(level.GetProperty("level").GetProperty("version").GetString()));
    }

    [SqlServerFact]
    public async Task Lessons_and_levels_are_reordered_with_the_full_list()
    {
        var admin = await SignInAsync(SystemRoles.ContentAdmin);
        var levelId = Id(await CreateLevelAsync(admin, "Numbers"));
        var ids = new List<string>();
        foreach (var title in new[] { "One", "Two", "Three" })
        {
            ids.Add((await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title })))
                .GetProperty("id").GetString()!);
        }

        var reordered = await admin.PutAsync(
            $"{LevelsRoute}/{levelId}/lessons/order",
            new { lessonIds = new[] { ids[2], ids[0], ids[1] } });
        var missing = await admin.PutAsync($"{LevelsRoute}/{levelId}/lessons/order", new { lessonIds = new[] { ids[0], ids[1] } });
        var repeated = await admin.PutAsync(
            $"{LevelsRoute}/{levelId}/lessons/order",
            new { lessonIds = new[] { ids[0], ids[0], ids[1] } });

        Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);
        Assert.Equal(["Three", "One", "Two"], Titles((await ReadAsync(reordered)).GetProperty("lessons")));
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);

        // Levels: the whole adventure, reversed.
        var levelIds = (await ListLevelsAsync(admin)).Select(level => level.GetProperty("id").GetString()!).ToList();
        levelIds.Reverse();
        var levels = await admin.PutAsync($"{LevelsRoute}/order", new { levelIds });

        Assert.Equal(HttpStatusCode.OK, levels.StatusCode);
        Assert.Equal(levelIds, (await ReadAsync(levels)).EnumerateArray().Select(level => level.GetProperty("id").GetString()!));
    }

    [SqlServerFact]
    public async Task A_lesson_is_published_corrected_unpublished_and_archived_for_good()
    {
        var admin = await SignInAsync(SystemRoles.ContentAdmin);
        var levelId = Id(await CreateLevelAsync(admin, "Food"));
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Fruit" }));
        var route = $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}";

        var published = await ReadAsync(await admin.PostAsync($"{route}/publish", new { version = Version(lesson) }));
        var corrected = await ReadAsync(await admin.PutAsync(route, new { title = "Fruits", version = Version(published) }));
        var stale = await admin.PutAsync(route, new { title = "Old", version = Version(published) });
        var unpublished = await ReadAsync(await admin.PostAsync($"{route}/unpublish", new { version = Version(corrected) }));
        var archived = await ReadAsync(await admin.PostAsync($"{route}/archive", new { version = Version(unpublished) }));
        var changeArchived = await admin.PostAsync($"{route}/publish", new { version = Version(archived) });
        var next = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Vegetables" }));
        var level = await ReadAsync(await admin.GetAsync($"{LevelsRoute}/{levelId}"));

        Assert.Equal("Published", published.GetProperty("status").GetString());
        Assert.Equal("Fruits", corrected.GetProperty("title").GetString());
        Assert.Equal("Published", corrected.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("Draft", unpublished.GetProperty("status").GetString());
        Assert.Equal("Archived", archived.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, changeArchived.StatusCode);
        // The archived lesson left the order: the next one takes position 1, and archived lessons come last.
        Assert.Equal(1, next.GetProperty("sortOrder").GetInt32());
        Assert.Equal(["Vegetables", "Fruits"], Titles(level.GetProperty("lessons")));
        Assert.Equal(1, level.GetProperty("level").GetProperty("lessons").GetProperty("archived").GetInt32());
    }

    [SqlServerFact]
    public async Task An_archived_level_freezes_its_lessons_and_leaves_the_default_list()
    {
        var admin = await SignInAsync(SystemRoles.ContentAdmin);
        var created = await CreateLevelAsync(admin, "Weather");
        var levelId = Id(created);
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Rain" }));

        var published = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/publish", new { version = Version(created.GetProperty("level")) }));
        var archived = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/archive", new { version = Version(published.GetProperty("level")) }));
        var addLesson = await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Snow" });
        var renameLesson = await admin.PutAsync(
            $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}",
            new { title = "Storm", version = Version(lesson) });
        var update = await admin.PutAsync($"{LevelsRoute}/{levelId}", new { title = "X", version = Version(archived.GetProperty("level")) });

        Assert.Equal("Published", published.GetProperty("level").GetProperty("status").GetString());
        Assert.Equal("Archived", archived.GetProperty("level").GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, addLesson.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, renameLesson.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, update.StatusCode);
        Assert.DoesNotContain(levelId, (await ListLevelsAsync(admin)).Select(level => level.GetProperty("id").GetString()));
        Assert.Contains(
            levelId,
            (await ListLevelsAsync(admin, includeArchived: true)).Select(level => level.GetProperty("id").GetString()));
    }

    [SqlServerFact]
    public async Task Each_action_needs_its_content_permission()
    {
        var parent = await SignInAsync(SystemRoles.Parent, UserType.Parent);
        var superAdmin = await SignInAsync(SystemRoles.SuperAdmin);
        await _fixture.GrantPermissionsAsync("ContentReader", Permissions.Content.ReadDrafts);
        var reader = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: "ContentReader"));
        var levelId = Id(await CreateLevelAsync(superAdmin, "Family"));
        var version = Version((await ReadAsync(await superAdmin.GetAsync($"{LevelsRoute}/{levelId}"))).GetProperty("level"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync(LevelsRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync(LevelsRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.PostAsync(LevelsRoute, new { title = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{LevelsRoute}/{levelId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync(LevelsRoute, new { title = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync($"{LevelsRoute}/{levelId}/publish", new { version })).StatusCode);
    }

    [SqlServerFact]
    public async Task Invalid_input_is_rejected_and_unknown_items_are_not_found()
    {
        var admin = await SignInAsync(SystemRoles.ContentAdmin);
        var levelId = Id(await CreateLevelAsync(admin, "Toys"));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(LevelsRoute, new { title = "  " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"{LevelsRoute}/{levelId}/publish", new { version = "nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{LevelsRoute}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{LessonsRoute}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await admin.PostAsync($"{LevelsRoute}/{Guid.NewGuid()}/lessons", new { title = "Ball" })).StatusCode);
    }

    private async Task<AdminApiClient> SignInAsync(string role, UserType userType = UserType.Admin)
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        return await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(userType, roles: role));
    }

    private static async Task<JsonElement> CreateLevelAsync(AdminApiClient admin, string title, string? description = null)
    {
        var response = await admin.PostAsync(LevelsRoute, new { title, description });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync(response);
    }

    private static async Task<List<JsonElement>> ListLevelsAsync(AdminApiClient admin, bool includeArchived = false)
    {
        var response = await admin.GetAsync(includeArchived ? $"{LevelsRoute}?includeArchived=true" : LevelsRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAsync(response)).EnumerateArray().ToList();
    }

    private static string Id(JsonElement levelDetails) => levelDetails.GetProperty("level").GetProperty("id").GetString()!;

    private static string Version(JsonElement item) => item.GetProperty("version").GetString()!;

    private static IEnumerable<string?> Titles(JsonElement items) =>
        items.EnumerateArray().Select(item => item.GetProperty("title").GetString());

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
