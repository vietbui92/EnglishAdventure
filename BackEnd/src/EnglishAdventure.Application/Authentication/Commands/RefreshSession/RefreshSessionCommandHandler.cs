using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Family;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.RefreshSession;

public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, TokenPair>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityUserService _identityUserService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IChildProfileRepository _childProfiles;

    public RefreshSessionCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIdentityUserService identityUserService,
        IAccessTokenIssuer accessTokenIssuer,
        IChildProfileRepository childProfiles)
    {
        _refreshTokenService = refreshTokenService;
        _identityUserService = identityUserService;
        _accessTokenIssuer = accessTokenIssuer;
        _childProfiles = childProfiles;
    }

    public async Task<TokenPair> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var rotationResult = await _refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

        switch (rotationResult)
        {
            case RefreshTokenRotationReuseDetected:
            case RefreshTokenRotationInvalid:
                throw new RefreshTokenException();
            case RefreshTokenRotationSucceeded succeeded:
            {
                var user = await _identityUserService.GetByIdAsync(succeeded.UserId, cancellationToken);
                if (user is null || !user.IsActive || user.IsLockedOut)
                {
                    // The user became ineligible during the session: end the session instead of leaving
                    // the freshly rotated token usable.
                    await _refreshTokenService.RevokeAsync(succeeded.NewRefreshToken, cancellationToken);
                    throw new RefreshTokenException();
                }

                if (succeeded.ChildProfileId is { } childProfileId)
                {
                    // A child session lasts only while its profile is active and still the Parent's (PD-010).
                    var profile = await _childProfiles.GetOwnedAsync(childProfileId, user.Id, cancellationToken);
                    if (profile is null || !profile.IsActive)
                    {
                        await _refreshTokenService.RevokeAsync(succeeded.NewRefreshToken, cancellationToken);
                        throw new RefreshTokenException();
                    }
                }

                var roles = succeeded.ChildProfileId is null
                    ? await _identityUserService.GetRolesAsync(user.Id, cancellationToken)
                    : [];
                var (accessToken, accessExpires) = _accessTokenIssuer.Issue(
                    user.Id,
                    user.UserType,
                    roles,
                    succeeded.ChildProfileId);

                return new TokenPair(
                    accessToken,
                    accessExpires,
                    succeeded.NewRefreshToken,
                    succeeded.NewRefreshTokenExpiresAtUtc);
            }
            default:
                throw new RefreshTokenException();
        }
    }
}
