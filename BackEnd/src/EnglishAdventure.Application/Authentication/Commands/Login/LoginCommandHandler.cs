using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenPair>
{
    private readonly IIdentityUserService _identityUserService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IIdentityUserService identityUserService,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService)
    {
        _identityUserService = identityUserService;
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<TokenPair> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _identityUserService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        switch (validationResult)
        {
            // Inactive is only reported after the password was verified, so it does not leak account existence.
            case CredentialValidationInactive:
                throw new AuthenticationForbiddenException("The account is not eligible to sign in.");
            // Lockout is checked before the password, so it shares the generic failure response.
            case CredentialValidationLockedOut:
            case CredentialValidationFailed:
                throw new AuthenticationFailedException();
            case CredentialValidationSucceeded succeeded:
            {
                var roles = await _identityUserService.GetRolesAsync(succeeded.User.Id, cancellationToken);
                var (accessToken, accessExpires) = _accessTokenIssuer.Issue(
                    succeeded.User.Id,
                    succeeded.User.AccountType,
                    roles);

                var (refreshToken, refreshExpires) = await _refreshTokenService.IssueAsync(
                    succeeded.User.Id,
                    cancellationToken);

                return new TokenPair(accessToken, accessExpires, refreshToken, refreshExpires);
            }
            default:
                throw new AuthenticationFailedException();
        }
    }
}
