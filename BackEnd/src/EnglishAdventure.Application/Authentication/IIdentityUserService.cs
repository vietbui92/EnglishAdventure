using EnglishAdventure.Application.Authentication.Models;

namespace EnglishAdventure.Application.Authentication;

public interface IIdentityUserService
{
    Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
}

public abstract record CredentialValidationResult;

public sealed record CredentialValidationSucceeded(AuthenticatedUser User) : CredentialValidationResult;

public sealed record CredentialValidationFailed : CredentialValidationResult;

public sealed record CredentialValidationLockedOut : CredentialValidationResult;

public sealed record CredentialValidationInactive : CredentialValidationResult;

/// <summary>The password is right, but a self-registered user has not confirmed the email yet.</summary>
public sealed record CredentialValidationEmailUnconfirmed : CredentialValidationResult;
