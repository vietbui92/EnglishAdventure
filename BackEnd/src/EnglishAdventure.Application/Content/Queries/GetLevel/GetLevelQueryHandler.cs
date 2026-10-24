using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLevel;

public sealed class GetLevelQueryHandler : IRequestHandler<GetLevelQuery, LevelDetails>
{
    private readonly IContentQueries _queries;

    public GetLevelQueryHandler(IContentQueries queries)
    {
        _queries = queries;
    }

    public async Task<LevelDetails> Handle(GetLevelQuery request, CancellationToken cancellationToken) =>
        await _queries.GetLevelAsync(request.LevelId, cancellationToken)
        ?? throw new NotFoundException("Level", request.LevelId);
}
