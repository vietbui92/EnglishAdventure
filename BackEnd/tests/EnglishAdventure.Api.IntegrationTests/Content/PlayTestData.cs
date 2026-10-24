using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>Published content for the play and attempt tests (S3.4, S4.1).</summary>
internal static class PlayTestData
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";
    private const string PlayRoute = "/api/v1/play";

    /// <param name="Admin">The ContentAdmin who built it.</param>
    /// <param name="WordIds">The lesson's words: cat, dog, fish.</param>
    public sealed record PublishedContent(
        AdminApiClient Admin,
        string LevelId,
        string LessonId,
        string BossId,
        string DraftLessonId,
        string DraftLevelId,
        List<string> WordIds);

    public static string Picture(string name) => $"https://cdn.example.test/{name}.png";

    /// <summary>
    /// Builds and publishes a level: lesson "Pets" (cat, dog, fish) with a quiz of three questions (picture → word for
    /// cat, word → picture for dog, "A ___ swims." for fish) and a matching of cat and dog with pictures, a draft lesson,
    /// and the boss "King Lion" matching all three words with their meanings. Also a draft level.
    /// </summary>
    public static async Task<PublishedContent> PublishLevelAsync(SqlServerAuthDatabaseFixture fixture)
    {
        await AdminApiClient.SyncSystemRolesAsync(fixture);
        var admin = await AdminApiClient.SignInAsync(fixture, await fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));
        var draftLevel = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Draft Land" }));
        var level = await ReadAsync(await admin.PostAsync(LevelsRoute, new { title = "Animal Island" }));
        var levelId = level.GetProperty("level").GetProperty("id").GetString()!;
        var lesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Pets" }));
        var draftLesson = await ReadAsync(await admin.PostAsync($"{LevelsRoute}/{levelId}/lessons", new { title = "Farm" }));
        var route = $"{LessonsRoute}/{lesson.GetProperty("id").GetString()}";

        var version = lesson.GetProperty("version").GetString()!;
        JsonElement bank = default;
        // The cat walks and meows on the discovery walk (S3.9); the others keep the defaults.
        foreach (var (english, meaning, movement, soundUrl) in new[]
                 {
                     ("cat", "con mèo", "Walk", "https://cdn.example.test/meow.mp3"),
                     ("dog", "con chó", null, null),
                     ("fish", "con cá", "Swim", null),
                 })
        {
            bank = await ReadAsync(await admin.PostAsync(
                $"{route}/words",
                new { english, meaning, imageUrl = Picture(english), movement, soundUrl, version }));
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

        // Earlier tests of the class published levels too. A new child starts on the first level (S4.2), so this one
        // goes first.
        var levelIds = (await ReadAsync(await admin.GetAsync(LevelsRoute))).EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            .ToList();
        var order = levelIds.Where(id => id != levelId).Prepend(levelId).ToArray();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsync($"{LevelsRoute}/order", new { levelIds = order })).StatusCode);

        return new PublishedContent(
            admin,
            levelId,
            lesson.GetProperty("id").GetString()!,
            boss.GetProperty("id").GetString()!,
            draftLesson.GetProperty("id").GetString()!,
            draftLevel.GetProperty("level").GetProperty("id").GetString()!,
            words);
    }

    /// <summary>
    /// Plays the whole "Pets" lesson as a child: the three questions (the first <paramref name="wrongAnswers"/> of them
    /// wrong) and the matching, every pair right at once. Returns the finished attempt: 5 items, 20% each.
    /// </summary>
    public static async Task<JsonElement> PlayPetsAsync(AdminApiClient child, string lessonId, int wrongAnswers = 0) =>
        (await PlayPetsWithRewardsAsync(child, lessonId, wrongAnswers)).GetProperty("attempt");

    /// <summary>Like <see cref="PlayPetsAsync"/>, but returns the last answer's whole result, with what it earned.</summary>
    public static async Task<JsonElement> PlayPetsWithRewardsAsync(AdminApiClient child, string lessonId, int wrongAnswers = 0)
    {
        var attempt = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{lessonId}/attempts"));
        var lesson = await ReadAsync(await child.GetAsync($"{PlayRoute}/lessons/{lessonId}"));
        var attemptRoute = $"{PlayRoute}/attempts/{attempt.GetProperty("attemptId").GetString()}";
        var questions = lesson.GetProperty("activities")[0].GetProperty("questions").EnumerateArray().ToList();
        string[] answers = ["cat", "dog", "fish"];
        for (var index = 0; index < questions.Count; index++)
        {
            var options = questions[index].GetProperty("options").EnumerateArray().ToList();
            var right = options.Single(option => option.GetProperty("text").GetString() == answers[index]
                                                 || option.GetProperty("imageUrl").GetString() == Picture(answers[index]));
            var chosen = index < wrongAnswers ? options.First(option => !option.Equals(right)) : right;
            var answered = await child.PostAsync(
                $"{attemptRoute}/questions/{questions[index].GetProperty("id").GetString()}",
                new { optionId = chosen.GetProperty("id").GetString() });
            Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
        }

        var matching = lesson.GetProperty("activities")[1].GetProperty("matching");
        JsonElement last = default;
        foreach (var word in new[] { "cat", "dog" })
        {
            var leftId = matching.GetProperty("left").EnumerateArray()
                .Single(choice => choice.GetProperty("text").GetString() == word).GetProperty("id").GetString();
            var rightId = matching.GetProperty("right").EnumerateArray()
                .Single(choice => choice.GetProperty("imageUrl").GetString() == Picture(word)).GetProperty("id").GetString();
            last = await ReadAsync(await child.PostAsync($"{attemptRoute}/matches", new { leftId, rightId }));
        }

        Assert.Equal("Finished", last.GetProperty("attempt").GetProperty("status").GetString());
        return last;
    }

    /// <summary>Publishes the draft lesson "Farm" (cow, pig, one question), after "Pets" in the level.</summary>
    public static async Task PublishFarmAsync(PublishedContent content)
    {
        var route = $"{LessonsRoute}/{content.DraftLessonId}";
        var version = (await ReadAsync(await content.Admin.GetAsync(route))).GetProperty("version").GetString()!;
        JsonElement bank = default;
        foreach (var (english, meaning) in new[] { ("cow", "con bò"), ("pig", "con lợn") })
        {
            bank = await ReadAsync(await content.Admin.PostAsync(
                $"{route}/words",
                new { english, meaning, imageUrl = Picture(english), version }));
            version = LessonVersion(bank);
        }

        var words = QuizTestData.WordIds(bank);
        var activities = await ReadAsync(await content.Admin.PostAsync($"{route}/activities/quiz", new { version }));
        var quizRoute = $"{route}/activities/{activities.GetProperty("activities")[0].GetProperty("id").GetString()}/questions";
        activities = await ReadAsync(await content.Admin.PostAsync(
            quizRoute,
            Question("PictureToWord", words[0], [words[1]], null, LessonVersion(activities))));
        var published = await content.Admin.PostAsync($"{route}/publish", new { version = LessonVersion(activities) });
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
    }

    private static object Question(string form, string correctWordId, string[] wrongWordIds, string? sentence, string version) =>
        new { form, correctWordId, wrongWordIds, sentence, version };

    private static string LessonVersion(JsonElement element) => element.GetProperty("lessonVersion").GetString()!;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
