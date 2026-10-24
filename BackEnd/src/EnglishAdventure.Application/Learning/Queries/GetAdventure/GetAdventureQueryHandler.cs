using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning.Models;
using EnglishAdventure.Application.Rewards;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetAdventure;

public sealed class GetAdventureQueryHandler : IRequestHandler<GetAdventureQuery, AdventureMap>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly IPlayQueries _queries;
    private readonly ILessonCompletionRepository _completions;
    private readonly IRewardsRepository _rewards;

    public GetAdventureQueryHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        IPlayQueries queries,
        ILessonCompletionRepository completions,
        IRewardsRepository rewards)
    {
        _userContext = userContext;
        _profiles = profiles;
        _queries = queries;
        _completions = completions;
        _rewards = rewards;
    }

    public async Task<AdventureMap> Handle(GetAdventureQuery request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        var (map, progress) = await LearningRules.ProgressAsync(_queries, _completions, childId, cancellationToken);
        return AdventureMap.From(map, progress, await _rewards.GetPointsAsync(childId, cancellationToken));
    }
}
