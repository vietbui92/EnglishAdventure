using EnglishAdventure.Application.Authentication.Models;

namespace EnglishAdventure.Api.Contracts.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshSessionRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokensResponse(
    string AccessToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc)
{
    public static AuthTokensResponse From(TokenPair tokens)
    {
        return new AuthTokensResponse(
            tokens.AccessToken,
            "Bearer",
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAtUtc);
    }
}

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string AccountType,
    IReadOnlyList<string> Roles)
{
    public static CurrentUserResponse From(CurrentUserProfile profile)
    {
        return new CurrentUserResponse(
            profile.UserId,
            profile.Email,
            profile.AccountType.ToString(),
            profile.Roles);
    }
}
