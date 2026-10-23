using System.Security.Cryptography;
using EnglishAdventure.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

public sealed class JwtConfigurationTests
{
    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        AccessTokenLifetimeMinutes = 15
    };

    [Fact]
    public void Validator_accepts_complete_options()
    {
        Assert.True(new JwtOptionsValidator().Validate(null, ValidOptions()).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!")]
    [InlineData("c2hvcnQta2V5")] // "short-key": valid base64 but under 256 bits
    public void Validator_rejects_a_missing_malformed_or_short_signing_key(string signingKey)
    {
        var options = ValidOptions();
        options.SigningKey = signingKey;

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("SigningKey", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Validator_rejects_an_out_of_range_access_token_lifetime(int minutes)
    {
        var options = ValidOptions();
        options.AccessTokenLifetimeMinutes = minutes;

        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void Host_fails_to_start_when_the_signing_key_is_not_configured()
    {
        using var factory = new EnglishAdventureWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Jwt:SigningKey"] = ""
                })));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("SigningKey", exception.Message);
    }
}
