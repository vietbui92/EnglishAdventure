using FluentValidation;

namespace EnglishAdventure.Application.Authentication;

/// <summary>
/// Password rule for Admin and Parent users (owner decision 2026-10-23): at least 12 characters with an upper-case
/// letter, a lower-case letter, a digit and a symbol. Messages never echo the password.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("A password is required.")
            .MinimumLength(MinimumLength).WithMessage($"The password must have at least {MinimumLength} characters.")
            .MaximumLength(MaximumLength).WithMessage($"The password cannot have more than {MaximumLength} characters.")
            .Must(password => password.Any(char.IsUpper)).WithMessage("The password needs an upper-case letter.")
            .Must(password => password.Any(char.IsLower)).WithMessage("The password needs a lower-case letter.")
            .Must(password => password.Any(char.IsDigit)).WithMessage("The password needs a digit.")
            .Must(password => password.Any(character => !char.IsLetterOrDigit(character)))
            .WithMessage("The password needs a symbol.");
}
