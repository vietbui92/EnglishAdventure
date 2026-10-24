using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Domain.Enums;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.ListBadgeDefinitions;

public sealed class ListBadgeDefinitionsQueryHandler
    : IRequestHandler<ListBadgeDefinitionsQuery, IReadOnlyList<BadgeDefinitionView>>
{
    private readonly IRewardsRepository _rewards;

    public ListBadgeDefinitionsQueryHandler(IRewardsRepository rewards)
    {
        _rewards = rewards;
    }

    public async Task<IReadOnlyList<BadgeDefinitionView>> Handle(
        ListBadgeDefinitionsQuery request,
        CancellationToken cancellationToken) =>
        (await _rewards.GetDefinitionsAsync(cancellationToken))
        .OrderBy(definition => definition.Kind)
        .Select(BadgeViews.Definition)
        .ToList();
}
