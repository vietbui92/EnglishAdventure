using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnglishAdventure.Api.Authentication;

public static class AuthenticationDependencyInjection
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Bearer validation reads the same validated JwtOptions that the token issuer uses.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;

                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwtOptions.CreateSigningKey(),
                    ValidAlgorithms = [JwtOptions.SigningAlgorithm],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = JwtOptions.ClockSkew,
                    NameClaimType = AuthClaimTypes.Subject,
                    RoleClaimType = AuthClaimTypes.Role
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, HttpContextUserContext>();

        return services;
    }
}
