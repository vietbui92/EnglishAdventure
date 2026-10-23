using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EnglishAdventure.Api.IntegrationTests;

public sealed class EnglishAdventureWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ConnectionStringKey = "ConnectionStrings:EnglishAdventure";

    /// <summary>
    /// Random per test run; never a real secret.
    /// </summary>
    public static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting, not ConfigureAppConfiguration: AddInfrastructure reads the connection string while Program.cs
        // builds the host, before ConfigureAppConfiguration overrides are applied.
        builder.UseSetting(
            ConnectionStringKey,
            "Server=(localdb)\\mssqllocaldb;Database=EnglishAdventure_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SigningKey"] = SigningKey,
                // Required outside Development, where appsettings.Development.json does not provide it.
                ["WebApp:BaseUrl"] = "http://localhost:5173",
                // Tests sign in far more often than a person would; the rate-limit tests set their own limits.
                ["RateLimiting:SignInPermitLimit"] = "100000",
                ["RateLimiting:RegistrationPermitLimit"] = "100000"
            });
        });
    }
}
