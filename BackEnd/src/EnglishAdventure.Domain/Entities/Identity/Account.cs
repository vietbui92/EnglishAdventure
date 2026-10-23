using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// A sign-in account (Admin, Parent or Child account type). Owns its password hash, its lockout state and the roles
/// assigned to it. Business entities of other modules refer to an account by <see cref="Entity.Id"/> only.
/// </summary>
public sealed class Account : Entity
{
    public const int EmailMaxLength = 256;
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<AccountRole> _roles = [];

    private Account()
    {
    }

    public string Email { get; private set; } = string.Empty;

    /// <summary>Upper-case form of <see cref="Email"/>; the unique lookup key.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public AccountType AccountType { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEndUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<AccountRole> Roles => _roles;

    public static Account Create(string email, string passwordHash, AccountType accountType, DateTime nowUtc)
    {
        var trimmedEmail = email?.Trim() ?? string.Empty;
        if (trimmedEmail.Length == 0 || trimmedEmail.Length > EmailMaxLength || !trimmedEmail.Contains('@'))
        {
            throw new DomainException("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        if (!Enum.IsDefined(accountType))
        {
            throw new DomainException($"Unknown account type '{accountType}'.");
        }

        return new Account
        {
            Id = Guid.NewGuid(),
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmail(trimmedEmail),
            PasswordHash = passwordHash,
            AccountType = accountType,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    /// <summary>
    /// Counts a wrong password. The <see cref="MaxFailedLoginAttempts"/>-th failure locks the account for
    /// <see cref="LockoutDuration"/> and starts a new count.
    /// </summary>
    public void RecordFailedLogin(DateTime nowUtc)
    {
        FailedLoginCount++;

        if (FailedLoginCount >= MaxFailedLoginAttempts)
        {
            LockoutEndUtc = nowUtc.Add(LockoutDuration);
            FailedLoginCount = 0;
        }
    }

    public void RecordSuccessfulLogin(DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new DomainException("An inactive account cannot sign in.");
        }

        FailedLoginCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = nowUtc;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool HasRole(Guid roleId) => _roles.Any(role => role.RoleId == roleId);

    /// <summary>Assigning a role the account already has changes nothing.</summary>
    public void AssignRole(Guid roleId, DateTime nowUtc, Guid? assignedByAccountId = null)
    {
        if (HasRole(roleId))
        {
            return;
        }

        _roles.Add(new AccountRole(Id, roleId, nowUtc, assignedByAccountId));
    }

    public void RemoveRole(Guid roleId) => _roles.RemoveAll(role => role.RoleId == roleId);
}
