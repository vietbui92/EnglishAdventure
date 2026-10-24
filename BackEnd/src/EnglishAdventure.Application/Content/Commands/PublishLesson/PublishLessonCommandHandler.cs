using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.PublishLesson;

public sealed class PublishLessonCommandHandler : IRequestHandler<PublishLessonCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PublishLessonCommandHandler(
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

    public async Task Handle(PublishLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.Publish(level, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
