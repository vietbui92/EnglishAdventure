using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Application.Family;

public interface IChildProfileRepository
{
    /// <summary>
    /// The profile when it belongs to <paramref name="parentUserId"/>, otherwise null: another family's profile is
    /// treated exactly like a missing one.
    /// </summary>
    Task<ChildProfile?> GetOwnedAsync(Guid profileId, Guid parentUserId, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(Guid parentUserId, CancellationToken cancellationToken);

    /// <summary>Whether another active profile of the family already uses the nickname (any letter case).</summary>
    Task<bool> NicknameInUseAsync(
        Guid parentUserId,
        Nickname nickname,
        Guid? exceptProfileId,
        CancellationToken cancellationToken);

    void Add(ChildProfile profile);
}
