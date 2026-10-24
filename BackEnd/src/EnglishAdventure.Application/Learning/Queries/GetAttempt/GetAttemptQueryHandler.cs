using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning.Models;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetAttempt;

public sealed class GetAttemptQueryHandler : IRequestHandler<GetAttemptQuery, AttemptView>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly ILessonRepository _lessons;
    private readonly ILessonAttemptRepository _attempts;

    public GetAttemptQueryHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        ILessonRepository lessons,
        ILessonAttemptRepository attempts)
    {
        _userContext = userContext;
        _profiles = profiles;
        _lessons = lessons;
        _attempts = attempts;
    }

    public async Task<AttemptView> Handle(GetAttemptQuery request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        var attempt = await LearningRules.GetOwnAttemptAsync(_attempts, request.AttemptId, childId, cancellationToken);
        var key = AnswerKey.For(await LearningRules.GetLessonOfAsync(_lessons, attempt, cancellationToken));
        return AttemptView.From(attempt, key);
    }
}
