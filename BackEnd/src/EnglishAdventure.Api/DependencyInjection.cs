using EnglishAdventure.Api.Authentication;
using EnglishAdventure.Api.Authorization;
using EnglishAdventure.Api.ExceptionHandling;
using EnglishAdventure.Api.RateLimiting;
using EnglishAdventure.Application;
using EnglishAdventure.Infrastructure;

namespace EnglishAdventure.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddEnglishAdventureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddApiExceptionHandling();
        services.AddApiAuthentication();
        services.AddApiAuthorization();
        services.AddApiRateLimiting(configuration);

        services.AddControllers();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
        });

        services.AddHealthChecks();

        return services;
    }
}
