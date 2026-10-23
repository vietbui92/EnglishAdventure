using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using Microsoft.AspNetCore.Identity;

namespace EnglishAdventure.Infrastructure.Identity;

internal sealed class IdentityUserService : IIdentityUserService
{
    // Hash verified when the email is unknown, so that response time does not reveal whether an account exists.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());

    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, password);
            return new CredentialValidationFailed();
        }

        // Checked before the password so that a locked account cannot be used as a password oracle.
        if (await _userManager.IsLockedOutAsync(user))
        {
            return new CredentialValidationLockedOut();
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return new CredentialValidationFailed();
        }

        if (!user.IsActive)
        {
            return new CredentialValidationInactive();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return new CredentialValidationSucceeded(ToAuthenticatedUser(user, isLockedOut: false));
    }

    public async Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        return ToAuthenticatedUser(user, await _userManager.IsLockedOutAsync(user));
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToArray();
    }

    private static AuthenticatedUser ToAuthenticatedUser(ApplicationUser user, bool isLockedOut)
    {
        return new AuthenticatedUser(
            user.Id,
            user.Email ?? string.Empty,
            user.AccountType,
            user.IsActive,
            isLockedOut);
    }
}
