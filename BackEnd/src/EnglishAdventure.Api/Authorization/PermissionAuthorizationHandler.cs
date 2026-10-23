using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EnglishAdventure.Api.Authorization;

/// <summary>
/// Grants a <see cref="PermissionRequirement"/> when the user in the access token currently holds the permission.
/// Permissions are read from the database (briefly cached), not from the token, so role changes apply quickly.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionReader _permissionReader;

    public PermissionAuthorizationHandler(IUserPermissionReader permissionReader)
    {
        _permissionReader = permissionReader;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject = context.User.FindFirst(AuthClaimTypes.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            return;
        }

        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        var permissions = await _permissionReader.GetPermissionsAsync(userId, cancellationToken);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
