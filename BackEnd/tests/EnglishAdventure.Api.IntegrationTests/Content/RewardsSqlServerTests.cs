using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Content;

/// <summary>
/// S5.1–S5.3 points and badges through the real API on SQL Server (PD-004, PD-011; owner decisions 2026-10-24): 10
/// points per newly earned star and each star once, the four badges once each, editable badge texts, and the child's
/// and Parent's views.
/// </summary>
public sealed class RewardsSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string PlayRoute = "/api/v1/play";
    private const string BadgesRoute = "/api/v1/admin/rewards/badges";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public RewardsSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task New_stars_earn_points_once_and_the_first_badges()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);

        // 4 of 5: two stars, the first lesson.
        var first = await PlayTestData.PlayPetsWithRewardsAsync(child, content.LessonId, wrongAnswers: 1);
        // All right: the third star, and perfect.
        var better = await PlayTestData.PlayPetsWithRewardsAsync(child, content.LessonId);
        // Worse: nothing.
        var worse = await PlayTestData.PlayPetsWithRewardsAsync(child, content.LessonId, wrongAnswers: 2);

        Assert.Equal(20, first.GetProperty("rewards").GetProperty("points").GetInt32());
        Assert.Equal(["FirstLesson"], Kinds(first.GetProperty("rewards").GetProperty("badges")));
        Assert.Equal("Bài học đầu tiên", first.GetProperty("rewards").GetProperty("badges")[0].GetProperty("name").GetString());
        Assert.Equal(10, better.GetProperty("rewards").GetProperty("points").GetInt32());
        Assert.Equal(["Perfect"], Kinds(better.GetProperty("rewards").GetProperty("badges")));
        Assert.Equal(JsonValueKind.Null, worse.GetProperty("rewards").ValueKind);

        var map = await ReadAsync(await child.GetAsync($"{PlayRoute}/map"));
        Assert.Equal(30, map.GetProperty("points").GetInt32());
        Assert.Equal(3, await CountAsync(db => db.PointsTransactions, content.LessonId));
    }

    [SqlServerFact]
    public async Task Defeating_the_boss_with_three_stars_everywhere_wins_both_level_badges()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        await PlayTestData.PlayPetsAsync(child, content.LessonId);

        var boss = await DefeatBossAsync(child, content.BossId);

        var earned = boss.GetProperty("rewards");
        Assert.Equal(30, earned.GetProperty("points").GetInt32());
        Assert.Equal(["LevelConqueror", "LevelSuperstar"], Kinds(earned.GetProperty("badges")));
        // The level has no description: its title names it.
        Assert.Equal("Chinh phục Animal Island", earned.GetProperty("badges")[0].GetProperty("name").GetString());
        Assert.Equal(content.LevelId, earned.GetProperty("badges")[0].GetProperty("levelId").GetString());

        var room = await ReadAsync(await child.GetAsync($"{PlayRoute}/rewards"));
        Assert.Equal(60, room.GetProperty("points").GetInt32());
        var badges = room.GetProperty("badges").EnumerateArray().ToList();
        Assert.All(badges.Where(badge => badge.GetProperty("levelId").GetString() == content.LevelId), badge =>
            Assert.True(badge.GetProperty("earned").GetBoolean()));
        Assert.All(badges.Where(badge => badge.GetProperty("levelId").ValueKind == JsonValueKind.Null), badge =>
            Assert.True(badge.GetProperty("earned").GetBoolean()));
    }

    [SqlServerFact]
    public async Task The_last_answer_sent_twice_at_once_pays_once()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        await PlayTestData.PlayPetsAsync(child, content.LessonId);
        var (attemptId, leftId, rightId) = await DefeatBossButLastAsync(child, content.BossId);

        var responses = await Task.WhenAll(
            child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId }),
            child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId }));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(3, await CountAsync(db => db.PointsTransactions, content.BossId));
        Assert.Equal(2, await CountAsync(db => db.BadgeAwards, content.BossId));
    }

    [SqlServerFact]
    public async Task A_Parent_sees_their_own_children_s_rewards_only()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var (parent, child, profileId) = await AdminApiClient.StartFamilyAsync(_fixture);
        var (otherParent, _, _) = await AdminApiClient.StartFamilyAsync(_fixture);
        await PlayTestData.PlayPetsAsync(child, content.LessonId);

        var own = await parent.GetAsync($"/api/v1/family/children/{profileId}/rewards");
        var other = await otherParent.GetAsync($"/api/v1/family/children/{profileId}/rewards");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(30, (await ReadAsync(own)).GetProperty("points").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync($"/api/v1/family/children/{profileId}/rewards")).StatusCode);
    }

    [SqlServerFact]
    public async Task A_SuperAdmin_edits_the_badges_and_children_see_the_new_texts()
    {
        var content = await PlayTestData.PublishLevelAsync(_fixture);
        var child = await AdminApiClient.StartChildSessionAsync(_fixture);
        var editor = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin));
        // ContentAdmins do not edit badges (S1.1: rewards.manage is SuperAdmin's).
        var contentAdmin = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.ContentAdmin));

        var list = await ReadAsync(await editor.GetAsync(BadgesRoute));
        var updated = await editor.PutAsync(
            $"{BadgesRoute}/LevelConqueror",
            new { name = " Vua {level} ", description = "Thắng boss của {level}.", imageUrl = "https://cdn.example.test/crown.png" });
        var blank = await editor.PutAsync($"{BadgesRoute}/Perfect", new { name = " ", description = "x" });
        var unknown = await editor.PutAsync($"{BadgesRoute}/Champion", new { name = "x", description = "x" });
        var http = await editor.PutAsync($"{BadgesRoute}/Perfect", new { name = "x", description = "x", imageUrl = "http://cdn.example.test/a.png" });

        Assert.Equal(["FirstLesson", "Perfect", "LevelConqueror", "LevelSuperstar"], Kinds(list));
        Assert.True(list[2].GetProperty("perLevel").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Vua {level}", (await ReadAsync(updated)).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync(BadgesRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await contentAdmin.GetAsync(BadgesRoute)).StatusCode);

        var room = await ReadAsync(await child.GetAsync($"{PlayRoute}/rewards"));
        var conqueror = room.GetProperty("badges").EnumerateArray()
            .Single(badge => badge.GetProperty("kind").GetString() == "LevelConqueror"
                             && badge.GetProperty("levelId").GetString() == content.LevelId);
        Assert.Equal("Vua Animal Island", conqueror.GetProperty("name").GetString());
        Assert.Equal("https://cdn.example.test/crown.png", conqueror.GetProperty("imageUrl").GetString());
        Assert.False(conqueror.GetProperty("earned").GetBoolean());
    }

    private static async Task<JsonElement> DefeatBossAsync(AdminApiClient child, string bossId)
    {
        var (attemptId, leftId, rightId) = await DefeatBossButLastAsync(child, bossId);
        return await ReadAsync(await child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId, rightId }));
    }

    /// <summary>Starts the boss (cat, dog and fish with their meanings) and matches all but the last pair.</summary>
    private static async Task<(string AttemptId, string LeftId, string RightId)> DefeatBossButLastAsync(
        AdminApiClient child,
        string bossId)
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
        foreach (var (left, right) in pairs[..^1])
        {
            Assert.Equal(
                HttpStatusCode.OK,
                (await child.PostAsync($"{PlayRoute}/attempts/{attemptId}/matches", new { leftId = left, rightId = right })).StatusCode);
        }

        return (attemptId, pairs[^1].Left, pairs[^1].Right);
    }

    private async Task<int> CountAsync<T>(Func<EnglishAdventureDbContext, IQueryable<T>> table, string lessonId)
        where T : class
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishAdventureDbContext>();
        var id = Guid.Parse(lessonId);
        return await table(dbContext).CountAsync(row => EF.Property<Guid>(row, "LessonId") == id);
    }

    private static List<string?> Kinds(JsonElement badges) =>
        badges.EnumerateArray().Select(badge => badge.GetProperty("kind").GetString()).ToList();

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
