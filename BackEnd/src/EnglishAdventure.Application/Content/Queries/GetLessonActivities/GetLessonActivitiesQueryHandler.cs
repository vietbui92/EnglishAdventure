using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLessonActivities;

public sealed class GetLessonActivitiesQueryHandler : IRequestHandler<GetLessonActivitiesQuery, LessonActivities>
{
    private readonly IContentQueries _queries;

    public GetLessonActivitiesQueryHandler(IContentQueries queries)
    {
        _queries = queries;
    }

    public async Task<LessonActivities> Handle(GetLessonActivitiesQuery request, CancellationToken cancellationToken) =>
        await _queries.GetLessonActivitiesAsync(request.LessonId, cancellationToken)
        ?? throw new NotFoundException("Lesson", request.LessonId);
}
