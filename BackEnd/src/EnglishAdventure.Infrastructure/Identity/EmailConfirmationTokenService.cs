using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Identity;

internal sealed class EmailConfirmationTokenService : IEmailConfirmationTokenService
{
    private readonly EnglishAdventureDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EmailConfirmationTokenService(EnglishAdventureDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Only the newest link works, so an older email cannot be used after the user asked for a new one.
        var earlier = await _dbContext.EmailConfirmationTokens
            .Where(token => token.UserId == userId && token.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        _dbContext.EmailConfirmationTokens.RemoveRange(earlier);

        // Same generator and hash as refresh tokens: 512 random bits, URL-safe, stored as SHA-256.
        var rawToken = RefreshTokenHasher.GenerateRawToken();
        var now = UtcNow();
        _dbContext.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshTokenHasher.Hash(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(EmailConfirmation.LinkLifetime)
        });

        return rawToken;
    }

    public async Task<Guid?> ConsumeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHasher.Hash(rawToken);
        var token = await _dbContext.EmailConfirmationTokens
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return null;
        }

        // A used token already confirmed its user, so opening the link again just confirms again (no change).
        if (token.UsedAtUtc is not null)
        {
            return token.UserId;
        }

        var now = UtcNow();
        if (token.ExpiresAtUtc <= now)
        {
            return null;
        }

        token.UsedAtUtc = now;
        return token.UserId;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
