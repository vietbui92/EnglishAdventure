using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Common.Models;
using MediatR;

namespace EnglishAdventure.Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserSummary>>
{
    private readonly IUserQueries _userQueries;

    public ListUsersQueryHandler(IUserQueries userQueries)
    {
        _userQueries = userQueries;
    }

    public Task<PagedResult<UserSummary>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        _userQueries.ListAsync(request.Page, request.PageSize, request.Search, cancellationToken);
}
