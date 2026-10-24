using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning.Models;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Learning.Commands.MatchPair;

public sealed class MatchPairCommandHandler : IRequestHandler<MatchPairCommand, AnswerResult>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly ILessonRepository _lessons;
    private readonly ILessonAttemptRepository _attempts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public MatchPairCommandHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        ILessonRepository lessons,
        ILessonAttemptRepository attempts,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userContext = userContext;
        _profiles = profiles;
        _lessons = lessons;
        _attempts = attempts;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<AnswerResult> Handle(MatchPairCommand request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        var attempt = await LearningRules.GetOwnAttemptAsync(_attempts, request.AttemptId, childId, cancellationToken);
        var key = AnswerKey.For(await LearningRules.GetLessonOfAsync(_lessons, attempt, cancellationToken));

        var outcome = attempt.MatchPair(key, request.LeftId, request.RightId, _timeProvider.GetUtcNow().UtcDateTime);
        if (!outcome.Repeated)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new AnswerResult(outcome.Correct, CorrectOptionId: null, AttemptView.From(attempt, key));
    }
}
