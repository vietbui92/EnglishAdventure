using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetPlayLesson;

public sealed class GetPlayLessonQueryHandler : IRequestHandler<GetPlayLessonQuery, PlayLesson>
{
    private readonly IPlayQueries _queries;

    public GetPlayLessonQueryHandler(IPlayQueries queries)
    {
        _queries = queries;
    }

    public async Task<PlayLesson> Handle(GetPlayLessonQuery request, CancellationToken cancellationToken) =>
        await _queries.GetLessonAsync(request.LessonId, cancellationToken)
        ?? throw new NotFoundException("Lesson", request.LessonId);
}
