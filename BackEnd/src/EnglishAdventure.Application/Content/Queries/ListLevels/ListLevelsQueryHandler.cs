using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.ListLevels;

public sealed class ListLevelsQueryHandler : IRequestHandler<ListLevelsQuery, IReadOnlyList<LevelSummary>>
{
    private readonly IContentQueries _queries;

    public ListLevelsQueryHandler(IContentQueries queries)
    {
        _queries = queries;
    }

    public Task<IReadOnlyList<LevelSummary>> Handle(ListLevelsQuery request, CancellationToken cancellationToken) =>
        _queries.ListLevelsAsync(request.IncludeArchived, cancellationToken);
}
