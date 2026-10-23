using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Commands.RefreshSession;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.UnitTests.Authentication;

public sealed class RefreshSessionCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime NewRefreshExpiresAtUtc = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeIdentityUserService _identityUserService = new();
    private readonly FakeAccessTokenIssuer _accessTokenIssuer = new();
    private readonly FakeRefreshTokenService _refreshTokenService = new();

    private RefreshSessionCommandHandler CreateHandler() =>
        new(_refreshTokenService, _identityUserService, _accessTokenIssuer);

    [Fact]
    public async Task Handle_returns_rotated_refresh_token_and_new_access_token()
    {
        _refreshTokenService.RotationResult = new RefreshTokenRotationSucceeded(UserId, "rotated", NewRefreshExpiresAtUtc);
        _identityUserService.User = new AuthenticatedUser(UserId, "child@example.test", AccountType.Child, true, false);

        var tokens = await CreateHandler().Handle(new RefreshSessionCommand("old"), CancellationToken.None);

        Assert.Equal("rotated", tokens.RefreshToken);
        Assert.Equal(NewRefreshExpiresAtUtc, tokens.RefreshTokenExpiresAtUtc);
        Assert.Equal($"access-{UserId}", tokens.AccessToken);
        Assert.Equal(AccountType.Child, Assert.Single(_accessTokenIssuer.Issued).AccountType);
    }

    [Fact]
    public async Task Handle_throws_when_reuse_is_detected()
    {
        _refreshTokenService.RotationResult = new RefreshTokenRotationReuseDetected();

        await Assert.ThrowsAsync<RefreshTokenException>(
            () => CreateHandler().Handle(new RefreshSessionCommand("old"), CancellationToken.None));

        Assert.Empty(_accessTokenIssuer.Issued);
    }

    [Fact]
    public async Task Handle_throws_when_token_is_invalid()
    {
        _refreshTokenService.RotationResult = new RefreshTokenRotationInvalid();

        await Assert.ThrowsAsync<RefreshTokenException>(
            () => CreateHandler().Handle(new RefreshSessionCommand("old"), CancellationToken.None));

        Assert.Empty(_accessTokenIssuer.Issued);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Handle_revokes_the_new_session_when_the_account_is_no_longer_eligible(bool isActive, bool isLockedOut)
    {
        _refreshTokenService.RotationResult = new RefreshTokenRotationSucceeded(UserId, "rotated", NewRefreshExpiresAtUtc);
        _identityUserService.User = new AuthenticatedUser(UserId, "p@example.test", AccountType.Parent, isActive, isLockedOut);

        await Assert.ThrowsAsync<RefreshTokenException>(
            () => CreateHandler().Handle(new RefreshSessionCommand("old"), CancellationToken.None));

        Assert.Equal(new[] { "rotated" }, _refreshTokenService.Revoked);
        Assert.Empty(_accessTokenIssuer.Issued);
    }
}
