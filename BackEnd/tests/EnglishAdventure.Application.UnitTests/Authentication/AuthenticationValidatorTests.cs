using EnglishAdventure.Application.Authentication.Commands.Login;
using EnglishAdventure.Application.Authentication.Commands.Logout;
using EnglishAdventure.Application.Authentication.Commands.RefreshSession;

namespace EnglishAdventure.Application.UnitTests.Authentication;

public sealed class AuthenticationValidatorTests
{
    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("parent@example.test", "")]
    public void Login_validator_rejects_missing_or_malformed_input(string email, string password)
    {
        var result = new LoginCommandValidator().Validate(new LoginCommand(email, password));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Login_validator_accepts_email_and_password()
    {
        var result = new LoginCommandValidator().Validate(new LoginCommand("parent@example.test", "password"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Refresh_and_logout_validators_reject_a_missing_token(string? refreshToken)
    {
        Assert.False(new RefreshSessionCommandValidator().Validate(new RefreshSessionCommand(refreshToken!)).IsValid);
        Assert.False(new LogoutCommandValidator().Validate(new LogoutCommand(refreshToken!)).IsValid);
    }

    [Fact]
    public void Refresh_and_logout_validators_reject_an_oversized_token()
    {
        var oversized = new string('a', 257);

        Assert.False(new RefreshSessionCommandValidator().Validate(new RefreshSessionCommand(oversized)).IsValid);
        Assert.False(new LogoutCommandValidator().Validate(new LogoutCommand(oversized)).IsValid);
    }
}
