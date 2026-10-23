using EnglishAdventure.Application.Authentication;
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

        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityInfrastructure();
        services.AddTokenInfrastructure(configuration);

        return services;
    }

    private static void AddIdentityInfrastructure(this IServiceCollection services)
    {
        // Identity core only: the API authenticates with JWT bearer tokens, not Identity cookies.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<EnglishAdventureDbContext>();

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
