using EnglishAdventure.Application.Authentication.Commands.ConfirmEmail;
using EnglishAdventure.Application.Authentication.Commands.RegisterParent;
using EnglishAdventure.Application.Authentication.Commands.ResendEmailConfirmation;

namespace EnglishAdventure.Application.UnitTests.Authentication;

public sealed class RegistrationValidatorTests
{
    private const string StrongPassword = "Correct-horse-1";

    [Theory]
    [InlineData(null)]
    [InlineData("vi")]
    [InlineData("en")]
    public void Register_validator_accepts_a_complete_request(string? language)
    {
        var result = new RegisterParentCommandValidator().Validate(
            new RegisterParentCommand("parent@example.test", StrongPassword, AcceptTerms: true, language));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_validator_requires_accepted_terms()
    {
        var result = new RegisterParentCommandValidator().Validate(
            new RegisterParentCommand("parent@example.test", StrongPassword, AcceptTerms: false, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterParentCommand.AcceptTerms));
    }

    [Theory]
    [InlineData("not-an-email", StrongPassword, "fr")]
    [InlineData("parent@example.test", "weak", null)]
    [InlineData("parent@example.test", StrongPassword, "fr")]
    public void Register_validator_rejects_bad_email_weak_password_or_unknown_language(
        string email,
        string password,
        string? language)
    {
        var result = new RegisterParentCommandValidator().Validate(
            new RegisterParentCommand(email, password, AcceptTerms: true, language));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Confirm_validator_requires_a_token(string token)
    {
        Assert.False(new ConfirmEmailCommandValidator().Validate(new ConfirmEmailCommand(token)).IsValid);
    }

    [Fact]
    public void Resend_validator_requires_a_valid_email()
    {
        Assert.False(new ResendEmailConfirmationCommandValidator().Validate(new ResendEmailConfirmationCommand("x", null)).IsValid);
        Assert.True(new ResendEmailConfirmationCommandValidator().Validate(new ResendEmailConfirmationCommand("a@b.test", "en")).IsValid);
    }
}
