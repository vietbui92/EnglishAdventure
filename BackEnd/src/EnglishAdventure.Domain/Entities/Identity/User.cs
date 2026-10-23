using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// A sign-in user (Admin, Parent or Child user type). Owns its password hash, its lockout state and the roles
/// assigned to it. Business entities of other modules refer to a user by <see cref="Entity.Id"/> only.
/// </summary>
public sealed class User : Entity
{
    public const int EmailMaxLength = 256;
    public const int ConsentVersionMaxLength = 32;
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserRole> _roles = [];

    private User()
    {
    }

    public string Email { get; private set; } = string.Empty;

    /// <summary>Upper-case form of <see cref="Email"/>; the unique lookup key.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserType UserType { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEndUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    /// <summary>When the user proved they own the email. Null until then; a self-registered user cannot sign in yet.</summary>
    public DateTime? EmailConfirmedAtUtc { get; private set; }

    /// <summary>When a self-registered Parent accepted the terms and privacy policy, and which version.</summary>
    public DateTime? ConsentAcceptedAtUtc { get; private set; }

    public string? ConsentVersion { get; private set; }

    public bool IsEmailConfirmed => EmailConfirmedAtUtc is not null;

    public IReadOnlyCollection<UserRole> Roles => _roles;

    /// <summary>
    /// A user created by an administrator. The administrator vouches for the email, so it counts as confirmed.
    /// </summary>
    public static User Create(string email, string passwordHash, UserType userType, DateTime nowUtc)
    {
        var user = NewUser(email, passwordHash, userType, nowUtc);
        user.EmailConfirmedAtUtc = nowUtc;
        return user;
    }

    /// <summary>
    /// A Parent who registered on the public site (PD-009). The email stays unconfirmed, so the user cannot sign in,
    /// until <see cref="ConfirmEmail"/>. Records the accepted consent version.
    /// </summary>
    public static User RegisterParent(string email, string passwordHash, string consentVersion, DateTime nowUtc)
    {
        var trimmedVersion = consentVersion?.Trim() ?? string.Empty;
        if (trimmedVersion.Length == 0 || trimmedVersion.Length > ConsentVersionMaxLength)
        {
            throw new DomainException("A consent version is required.");
        }

        var user = NewUser(email, passwordHash, UserType.Parent, nowUtc);
        user.ConsentAcceptedAtUtc = nowUtc;
        user.ConsentVersion = trimmedVersion;
        return user;
    }

    private static User NewUser(string email, string passwordHash, UserType userType, DateTime nowUtc)
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

        if (!Enum.IsDefined(userType))
        {
            throw new DomainException($"Unknown user type '{userType}'.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmail(trimmedEmail),
            PasswordHash = passwordHash,
            UserType = userType,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    /// <summary>
    /// Counts a wrong password. The <see cref="MaxFailedLoginAttempts"/>-th failure locks the user for
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
            throw new DomainException("An inactive user cannot sign in.");
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

    /// <summary>Confirming an already confirmed email changes nothing.</summary>
    public void ConfirmEmail(DateTime nowUtc) => EmailConfirmedAtUtc ??= nowUtc;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool HasRole(Guid roleId) => _roles.Any(role => role.RoleId == roleId);

    /// <summary>Assigning a role the user already has changes nothing.</summary>
    public void AssignRole(Guid roleId, DateTime nowUtc, Guid? assignedByUserId = null)
    {
        if (HasRole(roleId))
        {
            return;
        }

        _roles.Add(new UserRole(Id, roleId, nowUtc, assignedByUserId));
    }

    public void RemoveRole(Guid roleId) => _roles.RemoveAll(role => role.RoleId == roleId);
}
