using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.StartChildSession;

public sealed class StartChildSessionCommandHandler : IRequestHandler<StartChildSessionCommand, TokenPair>
{
    private readonly IChildProfileRepository _profiles;
    private readonly IIdentityUserService _identityUserService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserContext _userContext;

    public StartChildSessionCommandHandler(
        IChildProfileRepository profiles,
        IIdentityUserService identityUserService,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService,
        IUserContext userContext)
    {
        _profiles = profiles;
        _identityUserService = identityUserService;
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenService = refreshTokenService;
        _userContext = userContext;
    }

    public async Task<TokenPair> Handle(StartChildSessionCommand request, CancellationToken cancellationToken)
    {
        var parentId = FamilyRules.CurrentParentId(_userContext);
        var profile = await _profiles.GetOwnedAsync(request.ProfileId, parentId, cancellationToken)
            ?? throw new NotFoundException("ChildProfile", request.ProfileId);
        profile.EnsureCanLearn();

        var parent = await _identityUserService.GetByIdAsync(parentId, cancellationToken);
        if (parent is null || !parent.IsActive)
        {
            throw new UnauthorizedException();
        }

        // One session per device: the Parent's ends here, so coming back from child mode needs the password.
        if (!string.IsNullOrEmpty(request.CurrentRefreshToken))
        {
            await _refreshTokenService.RevokeAsync(request.CurrentRefreshToken, cancellationToken);
        }

        var (accessToken, accessExpires) = _accessTokenIssuer.Issue(
            parent.Id,
            parent.UserType,
            roles: [],
            childProfileId: profile.Id);
        var (refreshToken, refreshExpires) = await _refreshTokenService.IssueAsync(
            parent.Id,
            profile.Id,
            cancellationToken);

        return new TokenPair(accessToken, accessExpires, refreshToken, refreshExpires);
    }
}
