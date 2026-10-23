using EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;

namespace EnglishAdventure.Application.UnitTests.Authorization;

public sealed class BootstrapSuperAdminCommandValidatorTests
{
    private const string StrongPassword = "Adventure#2026";

    private readonly BootstrapSuperAdminCommandValidator _validator = new();

    [Fact]
    public void A_valid_email_and_strong_password_pass()
    {
        Assert.True(_validator.Validate(new BootstrapSuperAdminCommand("admin@example.test", StrongPassword)).IsValid);
    }

    [Theory]
    [InlineData("Short#1a", "at least 12 characters")]
    [InlineData("adventure#2026", "upper-case")]
    [InlineData("ADVENTURE#2026", "lower-case")]
    [InlineData("Adventure#Game", "digit")]
    [InlineData("Adventure2026x", "symbol")]
    public void Weak_passwords_are_rejected_with_a_clear_message(string password, string expectedMessagePart)
    {
        var result = _validator.Validate(new BootstrapSuperAdminCommand("admin@example.test", password));

        var error = Assert.Single(result.Errors);
        Assert.Contains(expectedMessagePart, error.ErrorMessage);
        Assert.DoesNotContain(password, error.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void An_invalid_email_is_rejected(string email)
    {
        Assert.False(_validator.Validate(new BootstrapSuperAdminCommand(email, StrongPassword)).IsValid);
    }

    [Fact]
    public void The_command_never_prints_its_password()
    {
        var command = new BootstrapSuperAdminCommand("admin@example.test", StrongPassword);

        Assert.DoesNotContain(StrongPassword, command.ToString());
        Assert.Contains("admin@example.test", command.ToString());
    }
}
