using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishAdventure.Api.IntegrationTests.Authorization;

/// <summary>
/// Permission policies through the real JWT and authorization middleware, without a database: permissions come from
/// <see cref="FixedPermissionReader"/>.
/// </summary>
public sealed class PermissionAuthorizationTests : IClassFixture<EnglishAdventureWebApplicationFactory>
{
    private const string PermissionCatalogRoute = "/api/v1/admin/permissions";

    private static readonly Guid RoleManager = Guid.NewGuid();
    private static readonly Guid ContentEditor = Guid.NewGuid();

    private readonly WebApplicationFactory<Program> _factory;

    public PermissionAuthorizationTests(EnglishAdventureWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserPermissionReader>();
            services.AddSingleton<IUserPermissionReader>(new FixedPermissionReader()
                .Grant(RoleManager, Permissions.Roles.Manage)
                .Grant(ContentEditor, Permissions.Content.Manage, Permissions.Content.Publish));
        }));
    }

    [Fact]
    public async Task A_protected_endpoint_returns_401_without_a_token()
    {
        var response = await _factory.CreateClient().GetAsync(PermissionCatalogRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_protected_endpoint_returns_403_problem_details_without_the_permission()
    {
        var response = await GetAsync(ContentEditor);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_role_in_the_token_does_not_grant_anything_by_itself()
    {
        // The token claims a SuperAdmin role, but permissions are read from the reader, which grants none.
        var response = await GetAsync(Guid.NewGuid(), SystemRoles.SuperAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_protected_endpoint_returns_200_with_the_permission()
    {
        var response = await GetAsync(RoleManager);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var catalog = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            Permissions.All.Select(permission => permission.Code),
            catalog.EnumerateArray().Select(permission => permission.GetProperty("code").GetString()));
    }

    private async Task<HttpResponseMessage> GetAsync(Guid userId, params string[] roles)
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();
        var (accessToken, _) = issuer.Issue(userId, UserType.Admin, roles);

        using var request = new HttpRequestMessage(HttpMethod.Get, PermissionCatalogRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _factory.CreateClient().SendAsync(request);
    }
}
