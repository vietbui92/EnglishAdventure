using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Family.Models;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.ValueObjects;
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

    public List<(Guid UserId, UserType UserType, IReadOnlyList<string> Roles, Guid? ChildProfileId)> Issued { get; } = new();

    public (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        UserType userType,
        IReadOnlyList<string> roles,
        Guid? childProfileId = null)
    {
        Issued.Add((userId, userType, roles, childProfileId));
        return ($"access-{userId}", ExpiresAtUtc);
    }
}

internal sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public static readonly DateTime ExpiresAtUtc = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public RefreshTokenRotationResult RotationResult { get; set; } = new RefreshTokenRotationInvalid();

    public List<Guid> IssuedFor { get; } = new();

    public List<string> Revoked { get; } = new();

    public Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(
        Guid userId,
        Guid? childProfileId,
        CancellationToken cancellationToken)
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

    public List<Guid> RevokedUsers { get; } = new();

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        RevokedUsers.Add(userId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserContext : IUserContext
{
    public Guid? UserId { get; set; }

    public Guid? ChildProfileId { get; set; }
}

internal sealed class FakeUserPermissionReader : IUserPermissionReader
{
    public HashSet<string> Permissions { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(Permissions);

    public Task<IReadOnlySet<string>> GetChildSessionPermissionsAsync(Guid parentUserId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(Application.Authorization.Permissions.SessionOnly.ToHashSet());
}

/// <summary>Child profiles in memory, for the session handlers.</summary>
internal sealed class FakeChildProfiles : IChildProfileRepository, IChildProfileQueries
{
    public List<ChildProfile> Profiles { get; } = new();

    public ChildProfile Add(Guid parentUserId, string nickname = "Bin")
    {
        var profile = ChildProfile.Create(parentUserId, Nickname.From(nickname), "fox", 0, DateTime.UtcNow);
        Profiles.Add(profile);
        return profile;
    }

    public Task<ChildProfile?> GetOwnedAsync(Guid profileId, Guid parentUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Profiles.SingleOrDefault(profile => profile.Id == profileId && profile.IsOwnedBy(parentUserId)));

    public Task<int> CountActiveAsync(Guid parentUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Profiles.Count(profile => profile.IsOwnedBy(parentUserId) && profile.IsActive));

    public Task<bool> NicknameInUseAsync(
        Guid parentUserId,
        Nickname nickname,
        Guid? exceptProfileId,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);

    void IChildProfileRepository.Add(ChildProfile profile) => Profiles.Add(profile);

    public Task<IReadOnlyList<ChildProfileSummary>> ListAsync(
        Guid parentUserId,
        bool includeArchived,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public async Task<ChildProfileSummary?> GetAsync(Guid profileId, Guid parentUserId, CancellationToken cancellationToken) =>
        await GetOwnedAsync(profileId, parentUserId, cancellationToken) is { } profile
            ? new ChildProfileSummary(profile.Id, profile.Nickname.Value, profile.AvatarKey, profile.Status.ToString(), profile.CreatedAtUtc, profile.ArchivedAtUtc)
            : null;
}
