using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Family;

/// <summary>
/// S2.1 child profiles (PD-010) through the real API on SQL Server: several profiles per Parent, at most three active,
/// unique nicknames within a family, archive and restore, and no access across families.
/// </summary>
public sealed class ChildProfilesSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string ChildrenRoute = "/api/v1/family/children";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public ChildProfilesSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task A_parent_creates_several_profiles_and_sees_them_in_order()
    {
        var parent = await SignInParentAsync();

        var first = await CreateAsync(parent, "Bin", "fox");
        var second = await CreateAsync(parent, "Bông", "owl");
        var list = await ListAsync(parent);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.NotNull(first.Headers.Location);
        var created = await ReadAsync(second);
        Assert.Equal("Bông", created.GetProperty("nickname").GetString());
        Assert.Equal("owl", created.GetProperty("avatarKey").GetString());
        Assert.Equal("Active", created.GetProperty("status").GetString());
        Assert.Equal(["Bin", "Bông"], list.Select(profile => profile.GetProperty("nickname").GetString()));
    }

    [SqlServerFact]
    public async Task A_fourth_active_profile_is_refused_until_one_is_archived()
    {
        var parent = await SignInParentAsync();
        var ids = new List<string>();
        foreach (var nickname in new[] { "An", "Bình", "Châu" })
        {
            ids.Add((await ReadAsync(await CreateAsync(parent, nickname, "cat"))).GetProperty("id").GetString()!);
        }

        var fourth = await CreateAsync(parent, "Dung", "cat");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fourth.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await parent.PostAsync($"{ChildrenRoute}/{ids[0]}/archive")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(parent, "Dung", "cat")).StatusCode);

        // The archived profile cannot come back while three are active.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await parent.PostAsync($"{ChildrenRoute}/{ids[0]}/restore")).StatusCode);
    }

    [SqlServerFact]
    public async Task Nicknames_are_unique_within_a_family_but_not_across_families()
    {
        var parent = await SignInParentAsync();
        var otherParent = await SignInParentAsync();
        await CreateAsync(parent, "Bin", "fox");

        var sameName = await CreateAsync(parent, "bin", "owl");
        var otherFamily = await CreateAsync(otherParent, "Bin", "owl");

        Assert.Equal(HttpStatusCode.Conflict, sameName.StatusCode);
        Assert.Equal(HttpStatusCode.Created, otherFamily.StatusCode);
    }

    [SqlServerFact]
    public async Task An_archived_profile_frees_its_nickname_and_keeps_out_of_the_default_list()
    {
        var parent = await SignInParentAsync();
        var id = (await ReadAsync(await CreateAsync(parent, "Bin", "fox"))).GetProperty("id").GetString();

        var archived = await ReadAsync(await parent.PostAsync($"{ChildrenRoute}/{id}/archive"));
        var reused = await CreateAsync(parent, "Bin", "owl");
        var activeOnly = await ListAsync(parent);
        var withArchived = await ListAsync(parent, includeArchived: true);
        var restore = await parent.PostAsync($"{ChildrenRoute}/{id}/restore");

        Assert.Equal("Archived", archived.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, archived.GetProperty("archivedAtUtc").ValueKind);
        Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
        Assert.Single(activeOnly);
        Assert.Equal(["Active", "Archived"], withArchived.Select(profile => profile.GetProperty("status").GetString()));
        // Restoring would give the family two children called Bin.
        Assert.Equal(HttpStatusCode.Conflict, restore.StatusCode);
    }

    [SqlServerFact]
    public async Task A_profile_is_updated_while_active_and_restored_with_the_same_id()
    {
        var parent = await SignInParentAsync();
        var id = (await ReadAsync(await CreateAsync(parent, "Bin", "fox"))).GetProperty("id").GetString();

        var updated = await parent.PutAsync($"{ChildrenRoute}/{id}", new { nickname = " Bông ", avatarKey = "panda" });
        await parent.PostAsync($"{ChildrenRoute}/{id}/archive");
        var updateArchived = await parent.PutAsync($"{ChildrenRoute}/{id}", new { nickname = "Bin", avatarKey = "fox" });
        var restored = await parent.PostAsync($"{ChildrenRoute}/{id}/restore");

        Assert.Equal("Bông", (await ReadAsync(updated)).GetProperty("nickname").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, updateArchived.StatusCode);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        var body = await ReadAsync(restored);
        Assert.Equal(id, body.GetProperty("id").GetString());
        Assert.Equal("Active", body.GetProperty("status").GetString());
    }

    [SqlServerFact]
    public async Task Another_family_cannot_see_or_change_a_profile()
    {
        var owner = await SignInParentAsync();
        var stranger = await SignInParentAsync();
        var id = (await ReadAsync(await CreateAsync(owner, "Bin", "fox"))).GetProperty("id").GetString();
        var route = $"{ChildrenRoute}/{id}";

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsync(route, new { nickname = "X", avatarKey = "owl" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"{route}/archive")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"{route}/restore")).StatusCode);
        Assert.Empty(await ListAsync(stranger));

        // The owner's profile is unchanged.
        var profile = await ReadAsync(await owner.GetAsync(route));
        Assert.Equal("Bin", profile.GetProperty("nickname").GetString());
        Assert.Equal("Active", profile.GetProperty("status").GetString());
    }

    [SqlServerFact]
    public async Task Only_parents_with_the_permission_can_use_the_endpoints()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var superAdmin = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync(ChildrenRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await superAdmin.GetAsync(ChildrenRoute)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await superAdmin.PostAsync(ChildrenRoute, new { nickname = "Bin", avatarKey = "fox" })).StatusCode);
    }

    [SqlServerFact]
    public async Task Invalid_input_is_rejected_and_the_options_list_the_avatars()
    {
        var parent = await SignInParentAsync();

        var blank = await CreateAsync(parent, "  ", "fox");
        var unknownAvatar = await CreateAsync(parent, "Bin", "dragon");
        var options = await ReadAsync(await parent.GetAsync($"{ChildrenRoute}/options"));

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownAvatar.StatusCode);
        Assert.Equal(3, options.GetProperty("maxActive").GetInt32());
        Assert.Equal(12, options.GetProperty("avatarKeys").GetArrayLength());
    }

    private async Task<AdminApiClient> SignInParentAsync()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var parent = await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent);
        return await AdminApiClient.SignInAsync(_fixture, parent);
    }

    private static Task<HttpResponseMessage> CreateAsync(AdminApiClient parent, string nickname, string avatarKey) =>
        parent.PostAsync(ChildrenRoute, new { nickname, avatarKey });

    private static async Task<List<JsonElement>> ListAsync(AdminApiClient parent, bool includeArchived = false)
    {
        var response = await parent.GetAsync(includeArchived ? $"{ChildrenRoute}?includeArchived=true" : ChildrenRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAsync(response)).EnumerateArray().ToList();
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
