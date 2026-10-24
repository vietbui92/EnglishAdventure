using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLesson;

public sealed class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLessonCommandHandler(ILevelRepository levels, ILessonRepository lessons, IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.Rename(level, ContentTitle.From(request.Title));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
