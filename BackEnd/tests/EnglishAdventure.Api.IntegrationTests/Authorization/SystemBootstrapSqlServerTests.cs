using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;
using EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishAdventure.Api.IntegrationTests.Authorization;

/// <summary>
/// S1.2 on SQL Server: syncing the system roles and creating the first SuperAdmin. The class gets its own throwaway
/// database, so it starts without roles or users.
/// </summary>
public sealed class SystemBootstrapSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string Email = "first.admin@example.test";
    private const string Password = "Adventure#2026";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public SystemBootstrapSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task Bootstrap_syncs_roles_creates_one_SuperAdmin_and_refuses_a_second_run()
    {
        // A weak password is rejected before anything is created.
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(new BootstrapSuperAdminCommand(Email, "weak")));

        var first = await SendAsync(new BootstrapSuperAdminCommand(Email, Password));

        Assert.Equal(new SyncSystemRolesResult(Permissions.All.Count, 3, 12), first.Sync);
        var grants = await LoadGrantsAsync();
        Assert.Equal(
            SystemRoles.All.SelectMany(role => role.Permissions.Select(code => $"{role.Name}:{code}")).Order(),
            grants.Order());

        var user = await _fixture.WithDbContextAsync(db => db.Users
            .Include(candidate => candidate.Roles)
            .SingleAsync(candidate => candidate.Id == first.UserId));
        Assert.Equal(UserType.Admin, user.UserType);
        Assert.Single(user.Roles);

        var second = await Assert.ThrowsAsync<ConflictException>(
            () => SendAsync(new BootstrapSuperAdminCommand("another.admin@example.test", Password)));
        Assert.Contains("already exists", second.Message);

        // Sync stays idempotent after the bootstrap.
        Assert.Equal(new SyncSystemRolesResult(0, 0, 0), await SendAsync(new SyncSystemRolesCommand()));

        // The new SuperAdmin can sign in and reach an admin endpoint; it holds no family permissions.
        var accessToken = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var me = await (await _fixture.Factory.CreateClient().SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();
        var permissions = me.GetProperty("permissions").EnumerateArray().Select(permission => permission.GetString()).ToList();

        Assert.Contains(Permissions.Roles.Manage, permissions);
        Assert.DoesNotContain(permissions, code => code!.StartsWith("family.", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.OK, await GetStatusAsync("/api/v1/admin/permissions", accessToken));
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private Task<List<string>> LoadGrantsAsync() =>
        _fixture.WithDbContextAsync(db => db.Roles
            .SelectMany(role => role.Permissions.Select(grant => new { role.Name, grant.PermissionId }))
            .Join(db.Permissions, grant => grant.PermissionId, permission => permission.Id, (grant, permission) => grant.Name + ":" + permission.Code)
            .ToListAsync());

    private async Task<string> LoginAsync()
    {
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/login", new { Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpStatusCode> GetStatusAsync(string route, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return (await _fixture.Factory.CreateClient().SendAsync(request)).StatusCode;
    }
}
