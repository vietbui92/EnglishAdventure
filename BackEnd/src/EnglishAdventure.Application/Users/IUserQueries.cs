using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Common.Models;

namespace EnglishAdventure.Application.Users;

/// <summary>
/// Read-only user views for the administration screens.
/// </summary>
public interface IUserQueries
{
    /// <summary>Users ordered by email; <paramref name="search"/> filters on part of the email.</summary>
    Task<PagedResult<UserSummary>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);

    Task<UserSummary?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
