using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Family;

/// <summary>
/// A child as a learner (PD-001, PD-010): the stable identity that owns all progress, points and badges. It is not an
/// account: no email, password, real name, birth date or photo. Owned by exactly one Parent user (OQ-013), who refers
/// to it by <see cref="ParentUserId"/> only.
/// </summary>
public sealed class ChildProfile : Entity
{
    /// <summary>How many active profiles one Parent may have (owner decision 2026-10-24). Archived ones do not count.</summary>
    public const int MaxActivePerParent = 3;

    public const int AvatarKeyMaxLength = 32;

    /// <summary>
    /// The drawn avatars a profile can use (owner decision 2026-10-24: a fixed set, never photos). The web app draws each
    /// key; adding one here needs a matching picture there.
    /// </summary>
    public static readonly IReadOnlyList<string> AvatarKeys =
    [
        "fox", "owl", "cat", "bear", "rabbit", "panda", "lion", "penguin", "elephant", "monkey", "koala", "turtle"
    ];

    private ChildProfile()
    {
    }

    public Guid ParentUserId { get; private set; }

    public Nickname Nickname { get; private set; } = null!;

    public string AvatarKey { get; private set; } = string.Empty;

    public ChildProfileStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ArchivedAtUtc { get; private set; }

    public bool IsActive => Status == ChildProfileStatus.Active;

    /// <param name="activeProfilesOfParent">How many active profiles the Parent already has.</param>
    public static ChildProfile Create(
        Guid parentUserId,
        Nickname nickname,
        string avatarKey,
        int activeProfilesOfParent,
        DateTime nowUtc)
    {
        if (parentUserId == Guid.Empty)
        {
            throw new DomainException("A child profile needs its Parent.");
        }

        EnsureRoomForOneMore(activeProfilesOfParent);

        return new ChildProfile
        {
            Id = Guid.NewGuid(),
            ParentUserId = parentUserId,
            Nickname = nickname,
            AvatarKey = ValidAvatar(avatarKey),
            Status = ChildProfileStatus.Active,
            CreatedAtUtc = nowUtc
        };
    }

    public bool IsOwnedBy(Guid userId) => ParentUserId == userId;

    /// <summary>Only an active profile can start or keep a child session.</summary>
    public void EnsureCanLearn()
    {
        if (!IsActive)
        {
            throw new DomainException("An archived child profile cannot learn. Restore it first.");
        }
    }

    public void Update(Nickname nickname, string avatarKey)
    {
        if (!IsActive)
        {
            throw new DomainException("An archived child profile cannot be changed. Restore it first.");
        }

        Nickname = nickname;
        AvatarKey = ValidAvatar(avatarKey);
    }

    /// <summary>Archiving an archived profile changes nothing.</summary>
    public void Archive(DateTime nowUtc)
    {
        if (!IsActive)
        {
            return;
        }

        Status = ChildProfileStatus.Archived;
        ArchivedAtUtc = nowUtc;
    }

    /// <summary>Restoring an active profile changes nothing.</summary>
    /// <param name="activeProfilesOfParent">How many active profiles the Parent already has.</param>
    public void Restore(int activeProfilesOfParent)
    {
        if (IsActive)
        {
            return;
        }

        EnsureRoomForOneMore(activeProfilesOfParent);
        Status = ChildProfileStatus.Active;
        ArchivedAtUtc = null;
    }

    private static void EnsureRoomForOneMore(int activeProfilesOfParent)
    {
        if (activeProfilesOfParent >= MaxActivePerParent)
        {
            throw new DomainException(
                $"A Parent can have at most {MaxActivePerParent} active child profiles. Archive one first.");
        }
    }

    public static bool IsKnownAvatar(string? avatarKey) => avatarKey is not null && AvatarKeys.Contains(avatarKey);

    private static string ValidAvatar(string avatarKey) =>
        IsKnownAvatar(avatarKey) ? avatarKey : throw new DomainException($"Unknown avatar '{avatarKey}'.");
}
