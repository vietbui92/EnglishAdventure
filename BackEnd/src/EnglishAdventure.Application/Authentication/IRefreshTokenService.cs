namespace EnglishAdventure.Application.Authentication;

public interface IRefreshTokenService
{
    /// <summary>Starts a new session (token family). With <paramref name="childProfileId"/> it is a child session.</summary>
    Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(
        Guid userId,
        Guid? childProfileId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically consumes the token and issues its replacement in the same family. Presenting a token that was
    /// already consumed or revoked returns <see cref="RefreshTokenRotationReuseDetected"/> and revokes the family.
    /// </summary>
    Task<RefreshTokenRotationResult> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the session (token family) the token belongs to. Unknown or already revoked tokens are ignored.
    /// </summary>
    Task RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session of the user (all token families). Used when a user is deactivated.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}

public abstract record RefreshTokenRotationResult;

/// <param name="ChildProfileId">Set when the session is a child session; the replacement token keeps it.</param>
public sealed record RefreshTokenRotationSucceeded(
    Guid UserId,
    Guid? ChildProfileId,
    string NewRefreshToken,
    DateTime NewRefreshTokenExpiresAtUtc) : RefreshTokenRotationResult;

public sealed record RefreshTokenRotationInvalid : RefreshTokenRotationResult;

public sealed record RefreshTokenRotationReuseDetected : RefreshTokenRotationResult;
