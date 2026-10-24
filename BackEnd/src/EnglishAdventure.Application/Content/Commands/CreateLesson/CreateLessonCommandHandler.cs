using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateLesson;

public sealed class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateLessonCommandHandler(
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

    public async Task<Guid> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);

        var lesson = Lesson.Create(
            level,
            ContentTitle.From(request.Title),
            await _lessons.MaxSortOrderAsync(level.Id, cancellationToken) + 1,
            _timeProvider.GetUtcNow().UtcDateTime);

        _lessons.Add(lesson);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return lesson.Id;
    }
}
