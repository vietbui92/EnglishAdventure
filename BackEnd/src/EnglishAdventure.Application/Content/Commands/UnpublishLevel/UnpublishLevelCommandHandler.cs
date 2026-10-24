using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLevel;

public sealed class UnpublishLevelCommandHandler : IRequestHandler<UnpublishLevelCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILearnerActivityReader _learnerActivity;
    private readonly IUnitOfWork _unitOfWork;

    public UnpublishLevelCommandHandler(
        ILevelRepository levels,
        ILearnerActivityReader learnerActivity,
        IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _learnerActivity = learnerActivity;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnpublishLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        _levels.ExpectVersion(level, ContentVersion.Decode(request.Version));

        level.Unpublish(await _learnerActivity.HasAnyLessonOfLevelBeenStartedAsync(level.Id, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
