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

    // Hash verified when the email is unknown, so that response time does not reveal whether a user exists.
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
        var normalizedEmail = User.NormalizeEmail(email);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _passwordHasher.Verify(_dummyPasswordHash.Value, password);
            return new CredentialValidationFailed();
        }

        // Checked before the password so that a locked user cannot be used as a password oracle.
        if (user.IsLockedOut(UtcNow()))
        {
            return new CredentialValidationLockedOut();
        }

        var verification = _passwordHasher.Verify(user.PasswordHash, password);
        if (verification == PasswordVerification.Failed)
        {
            await SaveAsync(user, current => current.RecordFailedLogin(UtcNow()), cancellationToken);
            return new CredentialValidationFailed();
        }

        if (!user.IsActive)
        {
            return new CredentialValidationInactive();
        }

        await SaveAsync(
            user,
            current =>
            {
                if (verification == PasswordVerification.SucceededRehashNeeded)
                {
                    current.ChangePasswordHash(_passwordHasher.Hash(password));
                }

                current.RecordSuccessfulLogin(UtcNow());
            },
            cancellationToken);

        return new CredentialValidationSucceeded(ToAuthenticatedUser(user, isLockedOut: false));
    }

    public async Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return user is null ? null : ToAuthenticatedUser(user, user.IsLockedOut(UtcNow()));
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Roles)
            .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Applies a login-state change and saves it. Concurrent logins to the same user can collide on the row
    /// version; the change is then re-applied to the freshly loaded user so that no failed attempt is lost.
    /// </summary>
    private async Task SaveAsync(User user, Action<User> change, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            change(user);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
            {
                await _dbContext.Entry(user).ReloadAsync(cancellationToken);
            }
        }
    }

    private static AuthenticatedUser ToAuthenticatedUser(User user, bool isLockedOut)
    {
        return new AuthenticatedUser(
            user.Id,
            user.Email,
            user.UserType,
            user.IsActive,
            isLockedOut);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
