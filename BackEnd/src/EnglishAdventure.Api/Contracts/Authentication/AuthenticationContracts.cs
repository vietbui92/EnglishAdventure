using EnglishAdventure.Application.Authentication.Models;

namespace EnglishAdventure.Api.Contracts.Authentication;

public sealed record LoginRequest(string Email, string Password);

/// <param name="AcceptTerms">The Parent accepts the terms and privacy policy; must be true.</param>
/// <param name="Language">"vi" (default) or "en": the language of the emails.</param>
public sealed record RegisterRequest(string Email, string Password, bool AcceptTerms, string? Language);

public sealed record ConfirmEmailRequest(string Token);

/// <param name="Language">"vi" (default) or "en": the language of the email.</param>
public sealed record ResendEmailConfirmationRequest(string Email, string? Language);

/// <param name="ChildProfileId">One of the signed-in Parent's active profiles.</param>
/// <param name="RefreshToken">
/// For clients that keep the refresh token themselves: the device's current one, which is ended. The web app leaves it
/// out; its cookie is used.
/// </param>
public sealed record StartChildSessionRequest(Guid ChildProfileId, string? RefreshToken);

public sealed record RefreshSessionRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

/// <param name="RefreshToken">Null for web clients: their refresh token is in the httpOnly cookie instead.</param>
public sealed record AuthTokensResponse(
    string AccessToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime RefreshTokenExpiresAtUtc)
{
    public static AuthTokensResponse From(TokenPair tokens, bool includeRefreshToken = true)
    {
        return new AuthTokensResponse(
            tokens.AccessToken,
            "Bearer",
            tokens.AccessTokenExpiresAtUtc,
            includeRefreshToken ? tokens.RefreshToken : null,
            tokens.RefreshTokenExpiresAtUtc);
    }
}

/// <param name="Session">"user" for a user's own session, "child" for a child session started by a Parent.</param>
/// <param name="ChildProfile">The child a child session learns as; null otherwise.</param>
public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string UserType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string Session,
    CurrentChildResponse? ChildProfile)
{
    public static CurrentUserResponse From(CurrentUserProfile profile)
    {
        return new CurrentUserResponse(
            profile.UserId,
            profile.Email,
            profile.UserType.ToString(),
            profile.Roles,
            profile.Permissions,
            profile.Session,
            profile.ChildProfile is { } child
                ? new CurrentChildResponse(child.Id, child.Nickname, child.AvatarKey)
                : null);
    }
}

public sealed record CurrentChildResponse(Guid Id, string Nickname, string AvatarKey);
