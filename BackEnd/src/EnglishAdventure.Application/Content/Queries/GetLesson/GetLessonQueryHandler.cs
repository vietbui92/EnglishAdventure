using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLesson;

public sealed class GetLessonQueryHandler : IRequestHandler<GetLessonQuery, LessonSummary>
{
    private readonly IContentQueries _queries;

    public GetLessonQueryHandler(IContentQueries queries)
    {
        _queries = queries;
    }

    public async Task<LessonSummary> Handle(GetLessonQuery request, CancellationToken cancellationToken) =>
        await _queries.GetLessonAsync(request.LessonId, cancellationToken)
        ?? throw new NotFoundException("Lesson", request.LessonId);
}
