using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLesson;

public sealed class ArchiveLessonCommandHandler : IRequestHandler<ArchiveLessonCommand>
{
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ArchiveLessonCommandHandler(ILessonRepository lessons, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ArchiveLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.Archive(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
