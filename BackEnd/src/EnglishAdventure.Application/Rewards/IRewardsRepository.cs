using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Services;

namespace EnglishAdventure.Application.Rewards;

public interface IRewardsRepository
{
    /// <summary>The child's points: the sum of their ledger.</summary>
    Task<int> GetPointsAsync(Guid childProfileId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BadgeAward>> GetAwardsAsync(Guid childProfileId, CancellationToken cancellationToken);

    /// <summary>The four badge definitions, tracked so they can be changed.</summary>
    Task<IReadOnlyList<BadgeDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken);

    Task<BadgeDefinition?> GetDefinitionAsync(BadgeKind kind, CancellationToken cancellationToken);

    void Add(PointsTransaction transaction);

    void Add(BadgeAward award);
}

/// <summary>The badges a child owns, as the reward rules compare them.</summary>
public static class OwnedBadges
{
    public static IReadOnlySet<BadgeWin> Of(IEnumerable<BadgeAward> awards) =>
        awards.Select(award => new BadgeWin(award.Kind, award.LevelId)).ToHashSet();
}
