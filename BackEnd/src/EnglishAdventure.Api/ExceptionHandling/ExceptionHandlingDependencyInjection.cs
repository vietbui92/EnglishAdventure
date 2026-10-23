namespace EnglishAdventure.Api.ExceptionHandling;

public static class ExceptionHandlingDependencyInjection
{
    public static IServiceCollection AddApiExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
