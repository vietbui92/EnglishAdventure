using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateBoss;

public sealed class CreateBossCommandHandler : IRequestHandler<CreateBossCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateBossCommandHandler(
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

    public async Task<Guid> Handle(CreateBossCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        if (await _lessons.HasBossAsync(level.Id, cancellationToken))
        {
            throw new DomainException(
                "The level already has a boss. Archive it to make a new one.",
                ContentErrorCodes.BossExists);
        }

        var boss = Lesson.CreateBoss(
            level,
            ContentTitle.From(request.Title),
            ImageUrl.FromOptional(request.ImageUrl),
            _timeProvider.GetUtcNow().UtcDateTime);

        _lessons.Add(boss);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return boss.Id;
    }
}
