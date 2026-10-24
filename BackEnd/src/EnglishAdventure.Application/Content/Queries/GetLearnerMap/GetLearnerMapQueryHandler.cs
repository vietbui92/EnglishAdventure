using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLearnerMap;

public sealed class GetLearnerMapQueryHandler : IRequestHandler<GetLearnerMapQuery, LearnerMap>
{
    private readonly IPlayQueries _queries;

    public GetLearnerMapQueryHandler(IPlayQueries queries)
    {
        _queries = queries;
    }

    public Task<LearnerMap> Handle(GetLearnerMapQuery request, CancellationToken cancellationToken) =>
        _queries.GetMapAsync(cancellationToken);
}
