using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.RemoveLessonWord;

public sealed class RemoveLessonWordCommandHandler : IRequestHandler<RemoveLessonWordCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RemoveLessonWordCommandHandler(
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

    public async Task Handle(RemoveLessonWordCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithWordAsync(_lessons, request.LessonId, request.WordId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.RemoveWord(
            level,
            request.WordId,
            _timeProvider.GetUtcNow().UtcDateTime,
            await _lessons.IsWordUsedByBossAsync(lesson.LevelId, request.WordId, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
