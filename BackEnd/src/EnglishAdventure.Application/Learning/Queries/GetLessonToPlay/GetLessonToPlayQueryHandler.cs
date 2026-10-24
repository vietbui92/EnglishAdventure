using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Family;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetLessonToPlay;

public sealed class GetLessonToPlayQueryHandler : IRequestHandler<GetLessonToPlayQuery, PlayLesson>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly IPlayQueries _queries;
    private readonly ILessonCompletionRepository _completions;
    private readonly ILessonAttemptRepository _attempts;

    public GetLessonToPlayQueryHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        IPlayQueries queries,
        ILessonCompletionRepository completions,
        ILessonAttemptRepository attempts)
    {
        _userContext = userContext;
        _profiles = profiles;
        _queries = queries;
        _completions = completions;
        _attempts = attempts;
    }

    public async Task<PlayLesson> Handle(GetLessonToPlayQuery request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        var lesson = await _queries.GetLessonAsync(request.LessonId, cancellationToken)
                     ?? throw new NotFoundException("Lesson", request.LessonId);
        await LearningRules.EnsureOpenAsync(_queries, _completions, _attempts, childId, lesson.Id, cancellationToken);
        return lesson;
    }
}
