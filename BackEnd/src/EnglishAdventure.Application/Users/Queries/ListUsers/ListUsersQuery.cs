using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Common.Models;
using MediatR;

namespace EnglishAdventure.Application.Users.Queries.ListUsers;

public sealed record ListUsersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<UserSummary>>;
