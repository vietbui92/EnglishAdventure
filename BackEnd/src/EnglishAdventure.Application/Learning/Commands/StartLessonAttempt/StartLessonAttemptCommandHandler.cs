using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Learning.Models;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Learning.Commands.StartLessonAttempt;

public sealed class StartLessonAttemptCommandHandler : IRequestHandler<StartLessonAttemptCommand, AttemptView>
{
    private readonly IUserContext _userContext;
    private readonly IChildProfileRepository _profiles;
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly ILessonAttemptRepository _attempts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public StartLessonAttemptCommandHandler(
        IUserContext userContext,
        IChildProfileRepository profiles,
        ILevelRepository levels,
        ILessonRepository lessons,
        ILessonAttemptRepository attempts,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userContext = userContext;
        _profiles = profiles;
        _levels = levels;
        _lessons = lessons;
        _attempts = attempts;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<AttemptView> Handle(StartLessonAttemptCommand request, CancellationToken cancellationToken)
    {
        var childId = await LearningRules.CurrentChildAsync(_userContext, _profiles, cancellationToken);
        var lesson = await LearningRules.GetPlayableLessonAsync(_lessons, _levels, request.LessonId, cancellationToken);
        var key = AnswerKey.For(lesson);

        // Coming back to a lesson continues it (PD-012). Two starts at once are stopped by a unique index (409).
        var attempt = await _attempts.GetInProgressAsync(childId, lesson.Id, cancellationToken);
        if (attempt is null)
        {
            attempt = LessonAttempt.Start(childId, lesson.Id, key, _timeProvider.GetUtcNow().UtcDateTime);
            _attempts.Add(attempt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return AttemptView.From(attempt, key);
    }
}
