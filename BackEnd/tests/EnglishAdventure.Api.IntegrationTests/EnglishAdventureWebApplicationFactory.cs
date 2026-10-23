using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EnglishAdventure.Api.IntegrationTests;

public sealed class EnglishAdventureWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Random per test run; never a real secret.
    /// </summary>
    public static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EnglishAdventure"] =
                    "Server=(localdb)\\mssqllocaldb;Database=EnglishAdventure_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Authentication:Jwt:SigningKey"] = SigningKey
            });
        });
    }
}
