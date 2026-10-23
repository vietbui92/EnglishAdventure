using Microsoft.AspNetCore.Authorization;

namespace EnglishAdventure.Api.Authorization;

public static class AuthorizationDependencyInjection
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Scoped: the handler reads permissions through the request's DbContext.
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
