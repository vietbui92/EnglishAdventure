using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Commands.Login;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.UnitTests.Authentication;

public sealed class LoginCommandHandlerTests
{
    private static readonly AuthenticatedUser ActiveParent =
        new(Guid.NewGuid(), "parent@example.test", AccountType.Parent, IsActive: true, IsLockedOut: false);

    private readonly FakeIdentityUserService _identityUserService = new();
    private readonly FakeAccessTokenIssuer _accessTokenIssuer = new();
    private readonly FakeRefreshTokenService _refreshTokenService = new();

    private LoginCommandHandler CreateHandler() =>
        new(_identityUserService, _accessTokenIssuer, _refreshTokenService);

    [Fact]
    public async Task Handle_returns_token_pair_for_valid_credentials()
    {
        _identityUserService.CredentialResult = new CredentialValidationSucceeded(ActiveParent);
        _identityUserService.Roles = new[] { "Parent" };

        var tokens = await CreateHandler().Handle(new LoginCommand(ActiveParent.Email, "pw"), CancellationToken.None);

        Assert.Equal($"access-{ActiveParent.Id}", tokens.AccessToken);
        Assert.Equal($"refresh-{ActiveParent.Id}", tokens.RefreshToken);
        Assert.Equal(FakeAccessTokenIssuer.ExpiresAtUtc, tokens.AccessTokenExpiresAtUtc);
        Assert.Equal(FakeRefreshTokenService.ExpiresAtUtc, tokens.RefreshTokenExpiresAtUtc);

        var issued = Assert.Single(_accessTokenIssuer.Issued);
        Assert.Equal(ActiveParent.Id, issued.UserId);
        Assert.Equal(AccountType.Parent, issued.AccountType);
        Assert.Equal(new[] { "Parent" }, issued.Roles);
    }

    [Fact]
    public async Task Handle_throws_authentication_failed_for_invalid_credentials()
    {
        _identityUserService.CredentialResult = new CredentialValidationFailed();

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateHandler().Handle(new LoginCommand("x@example.test", "pw"), CancellationToken.None));

        AssertNoTokensIssued();
    }

    [Fact]
    public async Task Handle_throws_the_same_generic_failure_for_a_locked_out_account()
    {
        _identityUserService.CredentialResult = new CredentialValidationLockedOut();

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateHandler().Handle(new LoginCommand("x@example.test", "pw"), CancellationToken.None));

        AssertNoTokensIssued();
    }

    [Fact]
    public async Task Handle_throws_forbidden_for_an_inactive_account()
    {
        _identityUserService.CredentialResult = new CredentialValidationInactive();

        await Assert.ThrowsAsync<AuthenticationForbiddenException>(
            () => CreateHandler().Handle(new LoginCommand("x@example.test", "pw"), CancellationToken.None));

        AssertNoTokensIssued();
    }

    private void AssertNoTokensIssued()
    {
        Assert.Empty(_accessTokenIssuer.Issued);
        Assert.Empty(_refreshTokenService.IssuedFor);
    }
}
