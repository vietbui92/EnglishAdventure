using System.Security.Claims;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishAdventure.Infrastructure.Authentication;

internal sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _signingCredentials = new SigningCredentials(_options.CreateSigningKey(), JwtOptions.SigningAlgorithm);
        _timeProvider = timeProvider;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        AccountType accountType,
        IReadOnlyList<string> roles)
    {
        if (!Enum.IsDefined(accountType))
        {
            throw new ArgumentOutOfRangeException(nameof(accountType), accountType, "Unknown account type.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaimTypes.Subject, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaimTypes.AccountType, accountType.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(AuthClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            SigningCredentials = _signingCredentials
        };

        return (_tokenHandler.CreateToken(descriptor), expiresAtUtc);
    }
}
