using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLevels;

public sealed class ReorderLevelsCommandHandler : IRequestHandler<ReorderLevelsCommand>
{
    private readonly ILevelRepository _levels;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderLevelsCommandHandler(ILevelRepository levels, IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReorderLevelsCommand request, CancellationToken cancellationToken)
    {
        var levels = await _levels.ListInOrderAsync(cancellationToken);
        await ContentRules.ReorderAsync(
            levels,
            request.LevelIds,
            (level, sortOrder) => level.MoveTo(sortOrder),
            _unitOfWork,
            cancellationToken);
    }
}
