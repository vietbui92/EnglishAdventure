using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.PublishLevel;

public sealed class PublishLevelCommandHandler : IRequestHandler<PublishLevelCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PublishLevelCommandHandler(
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

    public async Task Handle(PublishLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        _levels.ExpectVersion(level, ContentVersion.Decode(request.Version));

        level.Publish(
            _timeProvider.GetUtcNow().UtcDateTime,
            await _lessons.HasPublishedBossAsync(level.Id, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
