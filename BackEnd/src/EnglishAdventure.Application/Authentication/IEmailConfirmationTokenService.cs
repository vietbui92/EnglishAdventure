namespace EnglishAdventure.Application.Authentication;

/// <summary>
/// Tokens for the link that confirms a registered Parent's email. Only a hash is stored. Neither method
/// saves: the caller saves through <see cref="Common.Persistence.IUnitOfWork"/>, together with the user change.
/// </summary>
public interface IEmailConfirmationTokenService
{
    /// <summary>Creates a token for the user and voids the user's earlier unused ones. Returns the raw token.</summary>
    Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the token used and returns its user, or null when it is unknown or expired. A token that was already used
    /// returns its user again, so opening the link twice still shows the account as confirmed.
    /// </summary>
    Task<Guid?> ConsumeAsync(string rawToken, CancellationToken cancellationToken);
}

public static class EmailConfirmation
{
    /// <summary>How long a confirmation link works (owner decision 2026-10-23).</summary>
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);
}
