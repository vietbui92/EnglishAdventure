using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLesson;

public sealed class UnpublishLessonCommandHandler : IRequestHandler<UnpublishLessonCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly ILearnerActivityReader _learnerActivity;
    private readonly IUnitOfWork _unitOfWork;

    public UnpublishLessonCommandHandler(
        ILevelRepository levels,
        ILessonRepository lessons,
        ILearnerActivityReader learnerActivity,
        IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _lessons = lessons;
        _learnerActivity = learnerActivity;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnpublishLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.Unpublish(level, await _learnerActivity.HasLessonBeenStartedAsync(lesson.Id, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
