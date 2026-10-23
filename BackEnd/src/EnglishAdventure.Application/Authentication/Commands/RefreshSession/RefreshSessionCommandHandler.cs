using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.RefreshSession;

public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, TokenPair>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityUserService _identityUserService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;

    public RefreshSessionCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIdentityUserService identityUserService,
        IAccessTokenIssuer accessTokenIssuer)
    {
        _refreshTokenService = refreshTokenService;
        _identityUserService = identityUserService;
        _accessTokenIssuer = accessTokenIssuer;
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
                    // The account became ineligible during the session: end the session instead of leaving
                    // the freshly rotated token usable.
                    await _refreshTokenService.RevokeAsync(succeeded.NewRefreshToken, cancellationToken);
                    throw new RefreshTokenException();
                }

                var roles = await _identityUserService.GetRolesAsync(user.Id, cancellationToken);
                var (accessToken, accessExpires) = _accessTokenIssuer.Issue(
                    user.Id,
                    user.AccountType,
                    roles);

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
