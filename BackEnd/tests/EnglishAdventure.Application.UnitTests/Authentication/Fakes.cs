using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.UnitTests.Authentication;

internal sealed class FakeIdentityUserService : IIdentityUserService
{
    public CredentialValidationResult CredentialResult { get; set; } = new CredentialValidationFailed();

    public AuthenticatedUser? User { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken) => Task.FromResult(CredentialResult);

    public Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(User?.Id == userId ? User : null);

    public Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Roles);
}

internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAtUtc = new(2026, 1, 1, 0, 15, 0, DateTimeKind.Utc);

    public List<(Guid UserId, AccountType AccountType, IReadOnlyList<string> Roles)> Issued { get; } = new();

    public (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        AccountType accountType,
        IReadOnlyList<string> roles)
    {
        Issued.Add((userId, accountType, roles));
        return ($"access-{userId}", ExpiresAtUtc);
    }
}

internal sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public static readonly DateTime ExpiresAtUtc = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public RefreshTokenRotationResult RotationResult { get; set; } = new RefreshTokenRotationInvalid();

    public List<Guid> IssuedFor { get; } = new();

    public List<string> Revoked { get; } = new();

    public Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        IssuedFor.Add(userId);
        return Task.FromResult(($"refresh-{userId}", ExpiresAtUtc));
    }

    public Task<RefreshTokenRotationResult> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken) =>
        Task.FromResult(RotationResult);

    public Task RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        Revoked.Add(rawRefreshToken);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserContext : IUserContext
{
    public Guid? UserId { get; set; }
}
