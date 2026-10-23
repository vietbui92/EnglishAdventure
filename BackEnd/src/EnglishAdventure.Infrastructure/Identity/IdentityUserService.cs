using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Identity;

internal sealed class IdentityUserService : IIdentityUserService
{
    private const int MaxSaveAttempts = 3;

    private readonly EnglishAdventureDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    // Hash verified when the email is unknown, so that response time does not reveal whether an account exists.
    private readonly Lazy<string> _dummyPasswordHash;

    public IdentityUserService(
        EnglishAdventureDbContext dbContext,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _dummyPasswordHash = new Lazy<string>(() => passwordHasher.Hash(Guid.NewGuid().ToString()));
    }

    public async Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = Account.NormalizeEmail(email);
        var account = await _dbContext.Accounts
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (account is null)
        {
            _passwordHasher.Verify(_dummyPasswordHash.Value, password);
            return new CredentialValidationFailed();
        }

        // Checked before the password so that a locked account cannot be used as a password oracle.
        if (account.IsLockedOut(UtcNow()))
        {
            return new CredentialValidationLockedOut();
        }

        var verification = _passwordHasher.Verify(account.PasswordHash, password);
        if (verification == PasswordVerification.Failed)
        {
            await SaveAsync(account, current => current.RecordFailedLogin(UtcNow()), cancellationToken);
            return new CredentialValidationFailed();
        }

        if (!account.IsActive)
        {
            return new CredentialValidationInactive();
        }

        await SaveAsync(
            account,
            current =>
            {
                if (verification == PasswordVerification.SucceededRehashNeeded)
                {
                    current.ChangePasswordHash(_passwordHasher.Hash(password));
                }

                current.RecordSuccessfulLogin(UtcNow());
            },
            cancellationToken);

        return new CredentialValidationSucceeded(ToAuthenticatedUser(account, isLockedOut: false));
    }

    public async Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return account is null ? null : ToAuthenticatedUser(account, account.IsLockedOut(UtcNow()));
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(account => account.Id == userId)
            .SelectMany(account => account.Roles)
            .Join(_dbContext.Roles, accountRole => accountRole.RoleId, role => role.Id, (_, role) => role.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Applies a login-state change and saves it. Concurrent logins to the same account can collide on the row
    /// version; the change is then re-applied to the freshly loaded account so that no failed attempt is lost.
    /// </summary>
    private async Task SaveAsync(Account account, Action<Account> change, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            change(account);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
            {
                await _dbContext.Entry(account).ReloadAsync(cancellationToken);
            }
        }
    }

    private static AuthenticatedUser ToAuthenticatedUser(Account account, bool isLockedOut)
    {
        return new AuthenticatedUser(
            account.Id,
            account.Email,
            account.AccountType,
            account.IsActive,
            isLockedOut);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
