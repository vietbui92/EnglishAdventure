using EnglishAdventure.Application.Users;
using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Common.Models;
using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

internal sealed class UserQueries : IUserQueries
{
    private readonly EnglishAdventureDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserQueries(EnglishAdventureDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<UserSummary>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var users = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = User.NormalizeEmail(search);
            users = users.Where(user => user.NormalizedEmail.Contains(normalizedSearch));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var pageItems = await users
            .OrderBy(user => user.NormalizedEmail)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var summaries = await ToSummariesAsync(pageItems, cancellationToken);
        return new PagedResult<UserSummary>(summaries, page, pageSize, totalCount);
    }

    public async Task<UserSummary?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return user is null ? null : (await ToSummariesAsync([user], cancellationToken))[0];
    }

    private async Task<IReadOnlyList<UserSummary>> ToSummariesAsync(
        IReadOnlyList<User> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.Select(user => user.Id).ToList();

        var roleNames = (await _dbContext.Users
                .Where(user => userIds.Contains(user.Id))
                .SelectMany(user => user.Roles)
                .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .ToListAsync(cancellationToken))
            .ToLookup(row => row.UserId, row => row.Name);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return users
            .Select(user => new UserSummary(
                user.Id,
                user.Email,
                user.UserType.ToString(),
                user.IsActive,
                user.IsLockedOut(now),
                roleNames[user.Id].Order(StringComparer.Ordinal).ToList(),
                user.CreatedAtUtc,
                user.LastLoginAtUtc))
            .ToList();
    }
}
