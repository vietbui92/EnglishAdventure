using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserProfile>
{
    private readonly IUserContext _userContext;
    private readonly IIdentityUserService _identityUserService;

    public GetCurrentUserQueryHandler(
        IUserContext userContext,
        IIdentityUserService identityUserService)
    {
        _userContext = userContext;
        _identityUserService = identityUserService;
    }

    public async Task<CurrentUserProfile> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId is not { } userId)
        {
            throw new UnauthorizedException();
        }

        var user = await _identityUserService.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException();
        }

        var roles = await _identityUserService.GetRolesAsync(userId, cancellationToken);

        return new CurrentUserProfile(user.Id, user.Email, user.AccountType, roles);
    }
}
