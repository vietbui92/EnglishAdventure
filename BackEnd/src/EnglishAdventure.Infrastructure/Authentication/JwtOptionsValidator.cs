using Microsoft.Extensions.Options;

namespace EnglishAdventure.Infrastructure.Authentication;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{JwtOptions.SectionName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtOptions.SectionName}:Audience is required.");
        }

        if (options.AccessTokenLifetimeMinutes is < 1 or > 60)
        {
            failures.Add($"{JwtOptions.SectionName}:AccessTokenLifetimeMinutes must be between 1 and 60.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add(
                $"{JwtOptions.SectionName}:SigningKey is required. Set it with user secrets in Development, " +
                "or the Authentication__Jwt__SigningKey environment variable / Key Vault elsewhere.");
        }
        else if (!HasMinimumLength(options.SigningKey))
        {
            failures.Add(
                $"{JwtOptions.SectionName}:SigningKey must be base64-encoded and at least " +
                $"{JwtOptions.MinimumSigningKeyBytes} bytes ({JwtOptions.MinimumSigningKeyBytes * 8} bits) long.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool HasMinimumLength(string signingKey)
    {
        var buffer = new byte[signingKey.Length];
        return Convert.TryFromBase64String(signingKey, buffer, out var bytesWritten)
            && bytesWritten >= JwtOptions.MinimumSigningKeyBytes;
    }
}
