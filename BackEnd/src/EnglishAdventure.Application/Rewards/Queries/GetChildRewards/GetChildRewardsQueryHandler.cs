using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.GetChildRewards;

public sealed class GetChildRewardsQueryHandler : IRequestHandler<GetChildRewardsQuery, RewardsOverview>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly IRewardsRepository _rewards;
    private readonly IPlayQueries _queries;

    public GetChildRewardsQueryHandler(
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

    public async Task<RewardsOverview> Handle(GetChildRewardsQuery request, CancellationToken cancellationToken)
    {
        var parentId = _userContext.UserId ?? throw new UnauthorizedException();
        // Ownership is checked on the server (PD-001): another family's child is simply not found.
        var profile = await _profiles.GetOwnedAsync(request.ChildProfileId, parentId, cancellationToken)
                      ?? throw new NotFoundException("ChildProfile", request.ChildProfileId);
        return await RewardsReader.OverviewAsync(_rewards, _queries, profile.Id, cancellationToken);
    }
}
