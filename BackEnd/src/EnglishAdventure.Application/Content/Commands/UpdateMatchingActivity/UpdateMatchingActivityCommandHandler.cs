using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateMatchingActivity;

public sealed class UpdateMatchingActivityCommandHandler : IRequestHandler<UpdateMatchingActivityCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateMatchingActivityCommandHandler(
        ILevelRepository levels,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _levels = levels;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateMatchingActivityCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithActivityAsync(
            _lessons,
            request.LessonId,
            request.ActivityId,
            questionId: null,
            cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.UpdateMatching(
            level,
            request.ActivityId,
            MatchingModes.Parse(request.Mode),
            request.WordIds,
            _timeProvider.GetUtcNow().UtcDateTime,
            await ContentRules.LevelWordsForAsync(_lessons, lesson, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
