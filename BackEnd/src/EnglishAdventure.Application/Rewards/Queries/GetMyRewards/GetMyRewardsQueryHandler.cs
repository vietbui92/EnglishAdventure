using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning;
using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.GetMyRewards;

public sealed class GetMyRewardsQueryHandler : IRequestHandler<GetMyRewardsQuery, RewardsOverview>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly IRewardsRepository _rewards;
    private readonly IPlayQueries _queries;

    public GetMyRewardsQueryHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        IRewardsRepository rewards,
        IPlayQueries queries)
    {
        _userContext = userContext;
        _profiles = profiles;
        _rewards = rewards;
        _queries = queries;
    }

    public async Task<RewardsOverview> Handle(GetMyRewardsQuery request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        return await RewardsReader.OverviewAsync(_rewards, _queries, childId, cancellationToken);
    }
}
