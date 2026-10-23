using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Users.Queries.GetUser;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserSummary>
{
    private readonly IUserQueries _userQueries;

    public GetUserQueryHandler(IUserQueries userQueries)
    {
        _userQueries = userQueries;
    }

    public async Task<UserSummary> Handle(GetUserQuery request, CancellationToken cancellationToken) =>
        await _userQueries.GetAsync(request.UserId, cancellationToken)
        ?? throw new NotFoundException("User", request.UserId);
}
