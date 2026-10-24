using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Users;

/// <summary>
/// Small helper for calling the API as a signed-in user in SQL Server tests.
/// </summary>
internal sealed class AdminApiClient
{
    private readonly SqlServerAuthDatabaseFixture _fixture;
    private readonly string _accessToken;

    private AdminApiClient(SqlServerAuthDatabaseFixture fixture, string accessToken)
    {
        _fixture = fixture;
        _accessToken = accessToken;
    }

    public static async Task SyncSystemRolesAsync(SqlServerAuthDatabaseFixture fixture)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new SyncSystemRolesCommand());
    }

    public static async Task<AdminApiClient> SignInAsync(SqlServerAuthDatabaseFixture fixture, TestUser user)
    {
        var response = await LoginAsync(fixture, user.Email, user.Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new AdminApiClient(fixture, body.GetProperty("accessToken").GetString()!);
    }

    public static Task<HttpResponseMessage> LoginAsync(SqlServerAuthDatabaseFixture fixture, string email, string password) =>
        fixture.Factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    public Task<HttpResponseMessage> GetAsync(string route) => SendAsync(HttpMethod.Get, route, content: null);

    public Task<HttpResponseMessage> PostAsync(string route, object? body = null) =>
        SendAsync(HttpMethod.Post, route, body is null ? null : JsonContent.Create(body));

    public Task<HttpResponseMessage> PutAsync(string route, object body) =>
        SendAsync(HttpMethod.Put, route, JsonContent.Create(body));

    public Task<HttpResponseMessage> DeleteAsync(string route) => SendAsync(HttpMethod.Delete, route, content: null);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, route) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await _fixture.Factory.CreateClient().SendAsync(request);
    }
}
