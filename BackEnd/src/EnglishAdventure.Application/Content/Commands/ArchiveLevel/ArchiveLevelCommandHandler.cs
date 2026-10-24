using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLevel;

public sealed class ArchiveLevelCommandHandler : IRequestHandler<ArchiveLevelCommand>
{
    private readonly ILevelRepository _levels;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ArchiveLevelCommandHandler(ILevelRepository levels, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _levels = levels;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ArchiveLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        _levels.ExpectVersion(level, ContentVersion.Decode(request.Version));

        level.Archive(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
