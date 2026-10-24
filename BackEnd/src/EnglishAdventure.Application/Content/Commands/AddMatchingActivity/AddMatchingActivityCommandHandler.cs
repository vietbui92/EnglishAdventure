using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddMatchingActivity;

public sealed class AddMatchingActivityCommandHandler : IRequestHandler<AddMatchingActivityCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AddMatchingActivityCommandHandler(
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

    public async Task<Guid> Handle(AddMatchingActivityCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        var matching = lesson.AddMatching(
            level,
            MatchingModes.Parse(request.Mode),
            request.WordIds,
            _timeProvider.GetUtcNow().UtcDateTime,
            await ContentRules.LevelWordsForAsync(_lessons, lesson, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return matching.Id;
    }
}
