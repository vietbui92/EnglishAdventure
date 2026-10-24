using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessons;

public sealed class ReorderLessonsCommandHandler : IRequestHandler<ReorderLessonsCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderLessonsCommandHandler(ILevelRepository levels, ILessonRepository lessons, IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReorderLessonsCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        level.EnsureLessonsCanChange();

        var lessons = await _lessons.ListInOrderAsync(level.Id, cancellationToken);
        await ContentRules.ReorderAsync(
            lessons,
            request.LessonIds,
            (lesson, sortOrder) => lesson.MoveTo(level, sortOrder),
            _unitOfWork,
            cancellationToken);
    }
}
