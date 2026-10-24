using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateLevel;

public sealed class CreateLevelCommandHandler : IRequestHandler<CreateLevelCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateLevelCommandHandler(ILevelRepository levels, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _levels = levels;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = Level.Create(
            ContentTitle.From(request.Title),
            request.Description,
            await _levels.MaxSortOrderAsync(cancellationToken) + 1,
            _timeProvider.GetUtcNow().UtcDateTime);

        _levels.Add(level);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return level.Id;
    }
}
