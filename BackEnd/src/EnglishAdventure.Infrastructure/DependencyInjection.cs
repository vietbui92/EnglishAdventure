using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning;
using EnglishAdventure.Application.Rewards;
using EnglishAdventure.Infrastructure.Persistence.Repositories.Learning;
using EnglishAdventure.Infrastructure.Persistence.Repositories.Rewards;
using EnglishAdventure.Application.Users;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Database;
using EnglishAdventure.Application.Common.Email;
using EnglishAdventure.Infrastructure.Authentication;
using EnglishAdventure.Infrastructure.Email;
using EnglishAdventure.Infrastructure.Identity;
using EnglishAdventure.Infrastructure.Persistence;
using EnglishAdventure.Infrastructure.Persistence.Queries;
using EnglishAdventure.Infrastructure.Persistence.Repositories.Content;
using EnglishAdventure.Infrastructure.Persistence.Repositories.Family;
using EnglishAdventure.Infrastructure.Persistence.Repositories.Identity;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserQueries, UserQueries>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        services.AddScoped<IChildProfileRepository, ChildProfileRepository>();
        services.AddScoped<IChildProfileQueries, ChildProfileQueries>();

        services.AddScoped<ILevelRepository, LevelRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IContentQueries, ContentQueries>();
        services.AddScoped<IPlayQueries, PlayQueries>();
        services.AddScoped<ILearnerActivityReader, LearnerActivityReader>();

        services.AddScoped<ILessonAttemptRepository, LessonAttemptRepository>();
        services.AddScoped<ILessonCompletionRepository, LessonCompletionRepository>();
        services.AddScoped<IRewardsRepository, RewardsRepository>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityInfrastructure();
        services.AddTokenInfrastructure(configuration);
        services.AddEmailInfrastructure(configuration);

        return services;
    }

    private static void AddEmailInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebAppOptions>()
            .Bind(configuration.GetSection(WebAppOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                $"{WebAppOptions.SectionName}:BaseUrl must be the web app's absolute address, e.g. http://localhost:5173.")
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton<IAppLinks, WebAppLinks>();
        services.AddSingleton<IEmailSender, PickupDirectoryEmailSender>();
        services.AddScoped<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
    }

    private static void AddIdentityInfrastructure(this IServiceCollection services)
    {
        // Users, roles and permissions are our own identity-schema entities. Lockout rules live in the User
        // entity; only the password hashing algorithm comes from Microsoft.Extensions.Identity.Core.
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IIdentityUserService, IdentityUserService>();

        services.AddMemoryCache();
        services.AddScoped<IUserPermissionReader, UserPermissionReader>();
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
