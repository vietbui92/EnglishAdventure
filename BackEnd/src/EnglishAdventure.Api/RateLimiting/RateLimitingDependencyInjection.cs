using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Api.RateLimiting;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Sign-in attempts per client address per window.</summary>
    public int SignInPermitLimit { get; set; } = 10;

    /// <summary>Registrations, resent links and confirmations per client address per window.</summary>
    public int RegistrationPermitLimit { get; set; } = 5;

    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Limits the public auth endpoints per client address with ASP.NET Core's built-in rate limiter (S1.4, part of
/// S6.1). The per-user lockout in the User entity still applies on top of this.
/// </summary>
public static class RateLimitingDependencyInjection
{
    public const string SignInPolicy = "sign-in";
    public const string RegistrationPolicy = "registration";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection(AuthRateLimitOptions.SectionName))
            .Validate(
                options => options.SignInPermitLimit > 0 && options.RegistrationPermitLimit > 0 && options.WindowSeconds > 0,
                $"{AuthRateLimitOptions.SectionName} limits must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(SignInPolicy, context => FixedWindowPerAddress(context, options => options.SignInPermitLimit));
            limiter.AddPolicy(RegistrationPolicy, context => FixedWindowPerAddress(context, options => options.RegistrationPermitLimit));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Too many attempts from this address. Try again later.",
                        Instance = context.HttpContext.Request.Path
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindowPerAddress(
        HttpContext context,
        Func<AuthRateLimitOptions, int> permitLimit)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit(options),
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0
        });
    }
}
