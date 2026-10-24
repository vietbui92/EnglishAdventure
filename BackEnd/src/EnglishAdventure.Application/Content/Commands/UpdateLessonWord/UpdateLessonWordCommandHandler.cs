using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLessonWord;

public sealed class UpdateLessonWordCommandHandler : IRequestHandler<UpdateLessonWordCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateLessonWordCommandHandler(
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

    public async Task Handle(UpdateLessonWordCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithWordAsync(_lessons, request.LessonId, request.WordId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.UpdateWord(
            level,
            request.WordId,
            request.English,
            request.Meaning,
            ImageUrl.FromOptional(request.ImageUrl),
            _timeProvider.GetUtcNow().UtcDateTime,
            ContentRules.Discovery(request.Movement, request.SoundUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
