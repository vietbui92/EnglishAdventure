using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Infrastructure.Identity;

internal sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly EnglishAdventureDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly RefreshTokenOptions _options;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        EnglishAdventureDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<RefreshTokenOptions> options,
        ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var (rawToken, token) = CreateToken(userId, familyId: Guid.NewGuid(), now);

        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (rawToken, token.ExpiresAtUtc);
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var tokenHash = RefreshTokenHasher.Hash(rawRefreshToken);

        var current = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new { token.Id, token.UserId, token.FamilyId, token.ExpiresAtUtc, token.RevokedAtUtc })
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return new RefreshTokenRotationInvalid();
        }

        if (current.RevokedAtUtc is not null)
        {
            await RevokeReusedFamilyAsync(current.UserId, current.FamilyId, now, cancellationToken);
            return new RefreshTokenRotationReuseDetected();
        }

        if (current.ExpiresAtUtc <= now)
        {
            return new RefreshTokenRotationInvalid();
        }

        var (newRawToken, replacement) = CreateToken(current.UserId, current.FamilyId, now);

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            // Conditional update: only one request can move the token from "active" to "rotated". A concurrent
            // request blocks on the row lock until this transaction commits, then matches zero rows.
            var consumed = await _dbContext.RefreshTokens
                .Where(token => token.Id == current.Id && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.RevokedAtUtc, now)
                        .SetProperty(token => token.RevocationReason, RefreshTokenRevocationReason.Rotated)
                        .SetProperty(token => token.ReplacedByTokenId, replacement.Id),
                    cancellationToken);

            if (consumed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                await RevokeReusedFamilyAsync(current.UserId, current.FamilyId, now, cancellationToken);
                return new RefreshTokenRotationReuseDetected();
            }

            _dbContext.RefreshTokens.Add(replacement);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return new RefreshTokenRotationSucceeded(current.UserId, newRawToken, replacement.ExpiresAtUtc);
    }

    public async Task RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHasher.Hash(rawRefreshToken);

        var familyId = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return;
        }

        await RevokeFamilyAsync(familyId.Value, UtcNow(), RefreshTokenRevocationReason.Revoked, cancellationToken);
    }

    private async Task RevokeReusedFamilyAsync(Guid userId, Guid familyId, DateTime now, CancellationToken cancellationToken)
    {
        var revoked = await RevokeFamilyAsync(familyId, now, RefreshTokenRevocationReason.ReuseDetected, cancellationToken);

        _logger.LogWarning(
            "Refresh token reuse detected for user {UserId}, token family {FamilyId}. Revoked {RevokedCount} active token(s).",
            userId,
            familyId,
            revoked);
    }

    private Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTime now,
        RefreshTokenRevocationReason reason,
        CancellationToken cancellationToken)
    {
        return _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.RevocationReason, reason),
                cancellationToken);
    }

    private (string RawToken, RefreshToken Token) CreateToken(Guid userId, Guid familyId, DateTime now)
    {
        var rawToken = RefreshTokenHasher.GenerateRawToken();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = RefreshTokenHasher.Hash(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.LifetimeDays)
        };

        return (rawToken, token);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
