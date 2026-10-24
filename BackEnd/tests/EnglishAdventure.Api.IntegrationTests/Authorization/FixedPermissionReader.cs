using EnglishAdventure.Application.Authorization;

namespace EnglishAdventure.Api.IntegrationTests.Authorization;

/// <summary>
/// Test replacement for the database-backed permission reader: each user gets a fixed set of permissions.
/// </summary>
internal sealed class FixedPermissionReader : IUserPermissionReader
{
    private readonly Dictionary<Guid, IReadOnlySet<string>> _permissions = new();

    public FixedPermissionReader Grant(Guid userId, params string[] permissions)
    {
        _permissions[userId] = permissions.ToHashSet(StringComparer.Ordinal);
        return this;
    }

    public Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_permissions.GetValueOrDefault(userId) ?? new HashSet<string>());

    public Task<IReadOnlySet<string>> GetChildSessionPermissionsAsync(Guid parentUserId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(Permissions.SessionOnly.ToHashSet(StringComparer.Ordinal));
}
