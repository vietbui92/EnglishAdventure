using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnglishAdventure.Infrastructure.Identity;

/// <summary>
/// Resolves a user's permissions from its roles on each request. Results are cached for
/// <see cref="CacheDuration"/>, so role, permission and activation changes take effect within that time.
/// </summary>
internal sealed class UserPermissionReader : IUserPermissionReader
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>();

    private readonly EnglishAdventureDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public UserPermissionReader(EnglishAdventureDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    private static readonly IReadOnlySet<string> ChildSessionPermissions =
        Permissions.SessionOnly.ToHashSet(StringComparer.Ordinal);

    public async Task<IReadOnlySet<string>> GetChildSessionPermissionsAsync(
        Guid parentUserId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"child-session-parent-active:{parentUserId:N}";
        if (!_cache.TryGetValue(cacheKey, out bool parentActive))
        {
            parentActive = await _dbContext.Users.AnyAsync(user => user.Id == parentUserId && user.IsActive, cancellationToken);
            _cache.Set(cacheKey, parentActive, CacheDuration);
        }

        return parentActive ? ChildSessionPermissions : NoPermissions;
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"user-permissions:{userId:N}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        // A deactivated user keeps no permissions, even while its access token has not expired yet.
        var codes = await _dbContext.Users
            .Where(user => user.Id == userId && user.IsActive)
            .SelectMany(user => user.Roles)
            .Join(
                _dbContext.Roles.SelectMany(role => role.Permissions),
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(_dbContext.Permissions, permissionId => permissionId, permission => permission.Id, (_, permission) => permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlySet<string> permissions = codes.Count == 0
            ? NoPermissions
            : codes.ToHashSet(StringComparer.Ordinal);

        _cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }
}
