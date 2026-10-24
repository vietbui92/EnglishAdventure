using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S3.2 quiz activities (PD-011, PD-012) through the real API on SQL Server: quizzes and questions built from the word
/// bank, 400 for every badly shaped question, the limits, the structure lock and the lesson version.
/// </summary>
public sealed class QuizActivitiesSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LevelsRoute = "/api/v1/admin/content/levels";
    private const string LessonsRoute = "/api/v1/admin/content/lessons";
    private const string Picture = "https://cdn.example.test/picture.png";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public QuizActivitiesSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task Quizzes_and_questions_are_added_in_order_with_their_answers()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog", "fish");

        var quiz = await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version = lesson.Version });
        var activities = await ReadAsync(quiz);
        var quizId = ActivityId(activities, 0);
        activities = await ReadAsync(await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{quizId}/questions",
            Question("PictureToWord", lesson.Words[0], [lesson.Words[1], lesson.Words[2]], null, LessonVersion(activities))));
        var stale = await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{quizId}/questions",
            Question("WordToPicture", lesson.Words[1], [lesson.Words[0]], null, lesson.Version));
        activities = await ReadAsync(await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{quizId}/questions",
            Question("FillInTheBlank", lesson.Words[2], [lesson.Words[0]], " I have a ___. ", LessonVersion(activities))));
        var read = await ReadAsync(await lesson.Admin.GetAsync($"{lesson.Route}/activities"));

        Assert.Equal(HttpStatusCode.Created, quiz.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(4, read.GetProperty("maxActivities").GetInt32());
        Assert.Equal(10, read.GetProperty("maxQuestionsPerQuiz").GetInt32());
        Assert.Equal(LessonVersion(activities), LessonVersion(read));
        var activity = read.GetProperty("activities")[0];
        Assert.Equal("Quiz", activity.GetProperty("type").GetString());
        var questions = activity.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(["PictureToWord", "FillInTheBlank"], questions.Select(question => question.GetProperty("form").GetString()));
        Assert.Equal(lesson.Words[0], questions[0].GetProperty("correctWordId").GetString());
        Assert.Equal(
            [lesson.Words[1], lesson.Words[2]],
            questions[0].GetProperty("wrongWordIds").EnumerateArray().Select(id => id.GetString()));
        Assert.Equal("I have a ___.", questions[1].GetProperty("sentence").GetString());
    }

    [SqlServerFact]
    public async Task Badly_shaped_questions_are_refused_with_400()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog", "fish", "bird", "cow");
        var (quizId, version) = await AddQuizAsync(lesson);
        var route = $"{lesson.Route}/activities/{quizId}/questions";
        var words = lesson.Words;

        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [], null, version))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [words[1], words[2], words[3], words[4]], null, version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [words[1], words[1]], null, version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [words[0]], null, version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("Riddle", words[0], [words[1]], null, version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("FillInTheBlank", words[0], [words[1]], "No gap.", version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [words[1]], "A ___.", version))).StatusCode);

        // Well shaped, but a word of another lesson: the lesson's own rule.
        var stranger = await lesson.Admin.PostAsync(route, Question("PictureToWord", words[0], [Guid.NewGuid().ToString()], null, version));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stranger.StatusCode);
        Assert.Equal("question-invalid", await CodeAsync(stranger));

        var unknownQuiz = await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{Guid.NewGuid()}/questions",
            Question("PictureToWord", words[0], [words[1]], null, version));
        Assert.Equal(HttpStatusCode.NotFound, unknownQuiz.StatusCode);
    }

    [SqlServerFact]
    public async Task A_lesson_has_at_most_four_activities_and_used_words_stay()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog");
        var (quizId, version) = await AddQuizAsync(lesson);
        var activities = await ReadAsync(await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{quizId}/questions",
            Question("PictureToWord", lesson.Words[0], [lesson.Words[1]], null, version)));
        version = LessonVersion(activities);
        for (var count = 1; count < 4; count++)
        {
            version = LessonVersion(await ReadAsync(await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version })));
        }

        var fifth = await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version });
        var removeUsedWord = await lesson.Admin.DeleteAsync($"{lesson.Route}/words/{lesson.Words[1]}?version={Uri.EscapeDataString(version)}");

        Assert.Equal("activity-limit", await CodeAsync(fifth));
        Assert.Equal("word-in-use", await CodeAsync(removeUsedWord));
    }

    [SqlServerFact]
    public async Task Questions_and_activities_are_reordered_and_removed_in_a_draft()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog", "fish");
        var (quizId, version) = await AddQuizAsync(lesson);
        var route = $"{lesson.Route}/activities/{quizId}/questions";
        var activities = await ReadAsync(await lesson.Admin.PostAsync(route, Question("PictureToWord", lesson.Words[0], [lesson.Words[1]], null, version)));
        activities = await ReadAsync(await lesson.Admin.PostAsync(route, Question("PictureToWord", lesson.Words[1], [lesson.Words[2]], null, LessonVersion(activities))));
        activities = await ReadAsync(await lesson.Admin.PostAsync(route, Question("PictureToWord", lesson.Words[2], [lesson.Words[0]], null, LessonVersion(activities))));
        var ids = QuestionIds(activities);

        activities = await ReadAsync(await lesson.Admin.PutAsync($"{route}/order", new { questionIds = new[] { ids[2], ids[0], ids[1] }, version = LessonVersion(activities) }));
        var mismatch = await lesson.Admin.PutAsync($"{route}/order", new { questionIds = new[] { ids[0] }, version = LessonVersion(activities) });
        activities = await ReadAsync(await lesson.Admin.DeleteAsync($"{route}/{ids[0]}?version={Uri.EscapeDataString(LessonVersion(activities))}"));
        var reordered = QuestionIds(activities);

        activities = await ReadAsync(await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version = LessonVersion(activities) }));
        var secondQuiz = ActivityId(activities, 1);
        activities = await ReadAsync(await lesson.Admin.PutAsync(
            $"{lesson.Route}/activities/order",
            new { activityIds = new[] { secondQuiz, quizId }, version = LessonVersion(activities) }));
        var afterReorder = activities.GetProperty("activities").EnumerateArray().Select(activity => activity.GetProperty("id").GetString()).ToList();
        activities = await ReadAsync(await lesson.Admin.DeleteAsync($"{lesson.Route}/activities/{secondQuiz}?version={Uri.EscapeDataString(LessonVersion(activities))}"));

        Assert.Equal([ids[2], ids[1]], reordered);
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        Assert.Equal([secondQuiz, quizId], afterReorder);
        Assert.Equal(quizId, ActivityId(activities, 0));
        Assert.Equal(1, activities.GetProperty("activities")[0].GetProperty("sortOrder").GetInt32());
    }

    [SqlServerFact]
    public async Task A_published_lesson_needs_its_quiz_and_corrects_only_sentences()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog");
        var (quizId, version) = await AddQuizAsync(lesson);

        var emptyQuiz = await lesson.Admin.PostAsync($"{lesson.Route}/publish", new { version });
        var activities = await ReadAsync(await lesson.Admin.PostAsync(
            $"{lesson.Route}/activities/{quizId}/questions",
            Question("FillInTheBlank", lesson.Words[0], [lesson.Words[1]], "I has a ___.", version)));
        var published = await ReadAsync(await lesson.Admin.PostAsync($"{lesson.Route}/publish", new { version = LessonVersion(activities) }));
        var questionRoute = $"{lesson.Route}/activities/{quizId}/questions/{QuestionIds(activities)[0]}";
        var current = published.GetProperty("version").GetString()!;

        var corrected = await lesson.Admin.PutAsync(
            questionRoute,
            Question("FillInTheBlank", lesson.Words[0], [lesson.Words[1]], "I have a ___.", current));
        current = LessonVersion(await ReadAsync(corrected));
        var changeAnswer = await lesson.Admin.PutAsync(
            questionRoute,
            Question("FillInTheBlank", lesson.Words[1], [lesson.Words[0]], "I have a ___.", current));
        var addQuiz = await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version = current });

        Assert.Equal("lesson-not-ready", await CodeAsync(emptyQuiz));
        Assert.Equal("Published", published.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        Assert.Equal("lesson-structure-locked", await CodeAsync(changeAnswer));
        Assert.Equal("lesson-structure-locked", await CodeAsync(addQuiz));
    }

    [SqlServerFact]
    public async Task Changing_activities_needs_content_manage()
    {
        var lesson = await NewLessonWithWordsAsync("cat", "dog");
        await _fixture.GrantPermissionsAsync("QuizReader", Permissions.Content.ReadDrafts);
        var reader = await AdminApiClient.SignInAsync(_fixture, await _fixture.CreateUserAsync(UserType.Admin, roles: "QuizReader"));

        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{lesson.Route}/activities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync($"{lesson.Route}/activities/quiz", new { version = lesson.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync($"{lesson.Route}/activities")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await lesson.Admin.GetAsync($"{LessonsRoute}/{Guid.NewGuid()}/activities")).StatusCode);
    }

    private sealed record TestLesson(AdminApiClient Admin, string Route, string Version, List<string> Words);

    /// <summary>A ContentAdmin with a new draft lesson whose word bank has the given words, all with pictures.</summary>
    private async Task<TestLesson> NewLessonWithWordsAsync(params string[] words)
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
        foreach (var english in words)
        {
            bank = await ReadAsync(await admin.PostAsync(
                $"{route}/words",
                new { english, meaning = "nghĩa", imageUrl = Picture, version }));
            version = LessonVersion(bank);
        }

        return new TestLesson(admin, route, version, QuizTestData.WordIds(bank));
    }

    private static async Task<(string QuizId, string Version)> AddQuizAsync(TestLesson lesson)
    {
        var activities = await ReadAsync(await lesson.Admin.PostAsync($"{lesson.Route}/activities/quiz", new { version = lesson.Version }));
        return (ActivityId(activities, activities.GetProperty("activities").GetArrayLength() - 1), LessonVersion(activities));
    }

    private static object Question(string form, string correctWordId, string[] wrongWordIds, string? sentence, string version) =>
        new { form, correctWordId, wrongWordIds, sentence, version };

    private static string LessonVersion(JsonElement activities) => activities.GetProperty("lessonVersion").GetString()!;

    private static string ActivityId(JsonElement activities, int index) =>
        activities.GetProperty("activities")[index].GetProperty("id").GetString()!;

    private static List<string> QuestionIds(JsonElement activities) =>
        activities.GetProperty("activities")[0].GetProperty("questions").EnumerateArray()
            .Select(question => question.GetProperty("id").GetString()!)
            .ToList();

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
