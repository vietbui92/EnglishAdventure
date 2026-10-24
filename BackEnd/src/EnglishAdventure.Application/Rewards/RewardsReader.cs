using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Rewards.Models;

namespace EnglishAdventure.Application.Rewards;

internal static class RewardsReader
{
    /// <summary>A child's points and every badge on the published map, won or not.</summary>
    public static async Task<RewardsOverview> OverviewAsync(
        IRewardsRepository rewards,
        IPlayQueries queries,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        var points = await rewards.GetPointsAsync(childProfileId, cancellationToken);
        var awards = await rewards.GetAwardsAsync(childProfileId, cancellationToken);
        var definitions = await rewards.GetDefinitionsAsync(cancellationToken);
        var map = await queries.GetMapAsync(cancellationToken);
        return new RewardsOverview(points, BadgeViews.Overview(definitions, awards, map));
    }
}
