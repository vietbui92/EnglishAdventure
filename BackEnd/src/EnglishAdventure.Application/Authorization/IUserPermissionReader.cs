namespace EnglishAdventure.Application.Authorization;

public interface IUserPermissionReader
{
    /// <summary>
    /// The permission codes the user holds through its roles. Empty for an unknown or deactivated user.
    /// The result may be cached for a short time, so role changes take effect within that time.
    /// </summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// The permissions of a child session started by <paramref name="parentUserId"/>: exactly
    /// <see cref="Permissions.SessionOnly"/> while the Parent is active, none otherwise. Never the Parent's own ones.
    /// </summary>
    Task<IReadOnlySet<string>> GetChildSessionPermissionsAsync(Guid parentUserId, CancellationToken cancellationToken);
}
