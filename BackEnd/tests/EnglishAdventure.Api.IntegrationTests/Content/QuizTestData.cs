using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Users;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>Builds the content a lesson needs before it is published (S3.2: at least one quiz with a question).</summary>
internal static class QuizTestData
{
    /// <summary>
    /// Adds a quiz with one picture → word question to the lesson at <paramref name="lessonRoute"/>. Returns the
    /// activities and the lesson's new version.
    /// </summary>
    public static async Task<(JsonElement Activities, string Version)> AddQuizAsync(
        AdminApiClient admin,
        string lessonRoute,
        string version,
        string correctWordId,
        params string[] wrongWordIds)
    {
        var quizResponse = await admin.PostAsync($"{lessonRoute}/activities/quiz", new { version });
        Assert.Equal(HttpStatusCode.Created, quizResponse.StatusCode);
        var quiz = await quizResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activityId = quiz.GetProperty("activities").EnumerateArray().Last().GetProperty("id").GetString();

        var questionResponse = await admin.PostAsync(
            $"{lessonRoute}/activities/{activityId}/questions",
            new
            {
                form = "PictureToWord",
                correctWordId,
                wrongWordIds,
                sentence = (string?)null,
                version = quiz.GetProperty("lessonVersion").GetString(),
            });
        Assert.Equal(HttpStatusCode.Created, questionResponse.StatusCode);
        var activities = await questionResponse.Content.ReadFromJsonAsync<JsonElement>();
        return (activities, activities.GetProperty("lessonVersion").GetString()!);
    }

    /// <summary>The Ids of a word bank's words, in order.</summary>
    public static List<string> WordIds(JsonElement bank) =>
        bank.GetProperty("words").EnumerateArray().Select(word => word.GetProperty("id").GetString()!).ToList();
}
