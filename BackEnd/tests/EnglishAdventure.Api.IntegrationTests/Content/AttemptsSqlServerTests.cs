using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S4.1 lesson attempts through the real API on SQL Server (owner answers 2026-10-24): one question at a time, the
/// first answer counts; matching pairs tried until right, counting when right on the first try; the server grades and
/// gives the stars; a child resumes and only sees their own attempts.
/// </summary>
public sealed class AttemptsSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string PlayRoute = "/api/v1/play";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public AttemptsSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task Starting_again_resumes_the_unfinished_attempt()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        var first = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));
        var lesson = await PlayLessonAsync(child, content.LessonId);
        await AnswerAsync(child, first, Question(lesson, 0), CorrectOption(Question(lesson, 0), PlayTestData.Picture("cat"), "cat"));
        var resumed = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));

        Assert.Equal("InProgress", first.GetProperty("status").GetString());
        Assert.Equal(5, first.GetProperty("totalItems").GetInt32());
        Assert.Equal(AttemptId(first), AttemptId(resumed));
        Assert.Equal(1, resumed.GetProperty("doneItems").GetInt32());
        Assert.Equal(1, resumed.GetProperty("questions").GetArrayLength());
    }

    [SqlServerFact]
    public async Task The_server_grades_each_answer_and_the_first_one_counts()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        var attempt = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));
        var question = Question(await PlayLessonAsync(child, content.LessonId), 0);
        var right = CorrectOption(question, PlayTestData.Picture("cat"), "cat");
        var wrong = Options(question).First(option => option != right);

        // A score sent by the client is ignored: only the chosen option matters.
        var answered = await child.PostAsync(
            $"{PlayRoute}/attempts/{AttemptId(attempt)}/questions/{question.GetProperty("id").GetString()}",
            new { optionId = wrong, score = 100, correct = true });
        var first = await ReadAsync(answered);
        var again = await ReadAsync(await child.PostAsync(
            $"{PlayRoute}/attempts/{AttemptId(attempt)}/questions/{question.GetProperty("id").GetString()}",
            new { optionId = right }));

        Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
        Assert.False(first.GetProperty("correct").GetBoolean());
        Assert.Equal(right, first.GetProperty("correctOptionId").GetString());
        Assert.False(again.GetProperty("correct").GetBoolean());
        Assert.Equal(1, again.GetProperty("attempt").GetProperty("doneItems").GetInt32());
    }

    [SqlServerFact]
    public async Task A_whole_lesson_is_played_and_scored_with_stars()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        var attempt = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));
        var lesson = await PlayLessonAsync(child, content.LessonId);

        await AnswerAsync(child, attempt, Question(lesson, 0), CorrectOption(Question(lesson, 0), PlayTestData.Picture("cat"), "cat"));
        await AnswerAsync(child, attempt, Question(lesson, 1), CorrectOption(Question(lesson, 1), PlayTestData.Picture("dog"), "dog"));
        await AnswerAsync(child, attempt, Question(lesson, 2), CorrectOption(Question(lesson, 2), PlayTestData.Picture("fish"), "fish"));

        // Matching: cat tried with the dog picture first (wrong, then right), dog right at once.
        var matching = lesson.GetProperty("activities")[1].GetProperty("matching");
        var cat = Left(matching, "cat");
        var dog = Left(matching, "dog");
        var wrong = await MatchAsync(child, attempt, cat, Right(matching, PlayTestData.Picture("dog")));
        await MatchAsync(child, attempt, cat, Right(matching, PlayTestData.Picture("cat")));
        var last = await MatchAsync(child, attempt, dog, Right(matching, PlayTestData.Picture("dog")));

        Assert.False(wrong.GetProperty("correct").GetBoolean());
        Assert.Equal(JsonValueKind.Null, wrong.GetProperty("correctOptionId").ValueKind);
        var finished = last.GetProperty("attempt");
        Assert.Equal("Finished", finished.GetProperty("status").GetString());
        // 4 of 5 right on the first try: 80%, two stars.
        var result = finished.GetProperty("result");
        Assert.Equal(4, result.GetProperty("correctItems").GetInt32());
        Assert.Equal(80, result.GetProperty("scorePercent").GetInt32());
        Assert.Equal(2, result.GetProperty("stars").GetInt32());
        Assert.Equal(2, finished.GetProperty("matchedPairs").GetArrayLength());

        // Playing again starts a new attempt.
        var replay = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));
        Assert.NotEqual(AttemptId(attempt), AttemptId(replay));
        Assert.Equal(0, replay.GetProperty("doneItems").GetInt32());
    }

    [SqlServerFact]
    public async Task Answers_outside_the_lesson_and_other_children_s_attempts_are_refused()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        var other = await AdminApiClient.StartChildSessionAsync(_fixture);
        var attempt = await ReadAsync(await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts"));
        var lesson = await PlayLessonAsync(child, content.LessonId);
        var questionId = Question(lesson, 0).GetProperty("id").GetString();
        var optionOfAnotherQuestion = Options(Question(lesson, 1)).First();

        var invalid = await child.PostAsync($"{PlayRoute}/attempts/{AttemptId(attempt)}/questions/{questionId}", new { optionId = optionOfAnotherQuestion });
        var stranger = await other.GetAsync($"{PlayRoute}/attempts/{AttemptId(attempt)}");
        var strangerAnswer = await other.PostAsync(
            $"{PlayRoute}/attempts/{AttemptId(attempt)}/questions/{questionId}",
            new { optionId = Options(Question(lesson, 0)).First() });
        var draft = await child.PostAsync($"{PlayRoute}/lessons/{content.DraftLessonId}/attempts");
        var empty = await child.PostAsync($"{PlayRoute}/attempts/{AttemptId(attempt)}/matches", new { leftId = Guid.Empty, rightId = Guid.Empty });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal("answer-invalid", (await ReadAsync(invalid)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, strangerAnswer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [SqlServerFact]
    public async Task A_lesson_children_started_can_no_longer_be_unpublished()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        await child.PostAsync($"{PlayRoute}/lessons/{content.LessonId}/attempts");
        var lesson = await ReadAsync(await content.Admin.GetAsync($"/api/v1/admin/content/lessons/{content.LessonId}"));

        var unpublish = await content.Admin.PostAsync(
            $"/api/v1/admin/content/lessons/{content.LessonId}/unpublish",
            new { version = lesson.GetProperty("version").GetString() });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unpublish.StatusCode);
        Assert.Equal("content-started", (await ReadAsync(unpublish)).GetProperty("code").GetString());
    }

    private static async Task<JsonElement> PlayLessonAsync(AdminApiClient child, string lessonId) =>
        await ReadAsync(await child.GetAsync($"{PlayRoute}/lessons/{lessonId}"));

    private static JsonElement Question(JsonElement lesson, int index) =>
        lesson.GetProperty("activities")[0].GetProperty("questions")[index];

    private static IEnumerable<string> Options(JsonElement question) =>
        question.GetProperty("options").EnumerateArray().Select(option => option.GetProperty("id").GetString()!);

    /// <summary>The option a child who knows the word picks: the word itself, or its picture.</summary>
    private static string CorrectOption(JsonElement question, string picture, string english) =>
        question.GetProperty("options").EnumerateArray()
            .Single(option => option.GetProperty("text").GetString() == english
                              || option.GetProperty("imageUrl").GetString() == picture)
            .GetProperty("id").GetString()!;

    private static string Left(JsonElement matching, string english) =>
        matching.GetProperty("left").EnumerateArray()
            .Single(choice => choice.GetProperty("text").GetString() == english)
            .GetProperty("id").GetString()!;

    private static string Right(JsonElement matching, string picture) =>
        matching.GetProperty("right").EnumerateArray()
            .Single(choice => choice.GetProperty("imageUrl").GetString() == picture)
            .GetProperty("id").GetString()!;

    private static async Task<JsonElement> AnswerAsync(AdminApiClient child, JsonElement attempt, JsonElement question, string optionId)
    {
        var response = await child.PostAsync(
            $"{PlayRoute}/attempts/{AttemptId(attempt)}/questions/{question.GetProperty("id").GetString()}",
            new { optionId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync(response);
        Assert.True(result.GetProperty("correct").GetBoolean());
        return result;
    }

    private static async Task<JsonElement> MatchAsync(AdminApiClient child, JsonElement attempt, string leftId, string rightId)
    {
        var response = await child.PostAsync($"{PlayRoute}/attempts/{AttemptId(attempt)}/matches", new { leftId, rightId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    private static string AttemptId(JsonElement attempt) => attempt.GetProperty("attemptId").GetString()!;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
