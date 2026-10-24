using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLevel;

public sealed class UpdateLevelCommandHandler : IRequestHandler<UpdateLevelCommand>
{
    private readonly ILevelRepository _levels;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLevelCommandHandler(ILevelRepository levels, IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = await ContentRules.GetLevelAsync(_levels, request.LevelId, cancellationToken);
        _levels.ExpectVersion(level, ContentVersion.Decode(request.Version));

        level.Update(ContentTitle.From(request.Title), request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
