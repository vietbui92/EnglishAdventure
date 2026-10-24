using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLessonWords;

public sealed class GetLessonWordsQueryHandler : IRequestHandler<GetLessonWordsQuery, LessonWordBank>
{
    private readonly IContentQueries _queries;

    public GetLessonWordsQueryHandler(IContentQueries queries)
    {
        _queries = queries;
    }

    public async Task<LessonWordBank> Handle(GetLessonWordsQuery request, CancellationToken cancellationToken) =>
        await _queries.GetLessonWordBankAsync(request.LessonId, cancellationToken)
        ?? throw new NotFoundException("Lesson", request.LessonId);
}
