using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnglishAdventure.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools (run with Infrastructure as both project and startup project).
/// Reads <c>ConnectionStrings__EnglishAdventure</c> from the environment and otherwise falls back to the local
/// LocalDB development database. Creating a migration does not connect; <c>database update</c> does.
/// </summary>
internal sealed class EnglishAdventureDbContextFactory : IDesignTimeDbContextFactory<EnglishAdventureDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=EnglishAdventure;Trusted_Connection=True;TrustServerCertificate=True;";

    public EnglishAdventureDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            $"ConnectionStrings__{DependencyInjection.ConnectionStringName}");

        var options = new DbContextOptionsBuilder<EnglishAdventureDbContext>()
            .UseSqlServer(
                string.IsNullOrWhiteSpace(connectionString) ? LocalDevelopmentConnectionString : connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(EnglishAdventureDbContext).Assembly.FullName))
            .Options;

        return new EnglishAdventureDbContext(options);
    }
}
