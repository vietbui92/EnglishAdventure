using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Database;
using EnglishAdventure.Infrastructure.Authentication;
using EnglishAdventure.Infrastructure.Identity;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "EnglishAdventure";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Set it in appsettings, user secrets, or the ConnectionStrings__EnglishAdventure environment variable.");
        }

        services.AddDbContext<EnglishAdventureDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(EnglishAdventureDbContext).Assembly.FullName)));

        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityInfrastructure();
        services.AddTokenInfrastructure(configuration);

        return services;
    }

    private static void AddIdentityInfrastructure(this IServiceCollection services)
    {
        // Accounts, roles and permissions are our own identity-schema entities. Lockout rules live in the Account
        // entity; only the password hashing algorithm comes from Microsoft.Extensions.Identity.Core.
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IIdentityUserService, IdentityUserService>();
    }

    private static void AddTokenInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .Validate(
                options => options.LifetimeDays is >= 1 and <= 90,
                $"{RefreshTokenOptions.SectionName}:LifetimeDays must be between 1 and 90.")
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    }
}
