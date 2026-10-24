using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Commands.UpdateBadgeDefinition;

public sealed class UpdateBadgeDefinitionCommandHandler
    : IRequestHandler<UpdateBadgeDefinitionCommand, BadgeDefinitionView>
{
    private readonly IRewardsRepository _rewards;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBadgeDefinitionCommandHandler(IRewardsRepository rewards, IUnitOfWork unitOfWork)
    {
        _rewards = rewards;
        _unitOfWork = unitOfWork;
    }

    public async Task<BadgeDefinitionView> Handle(UpdateBadgeDefinitionCommand request, CancellationToken cancellationToken)
    {
        var kind = BadgeKinds.Parse(request.Kind);
        var definition = await _rewards.GetDefinitionAsync(kind, cancellationToken)
                         ?? throw new NotFoundException("BadgeDefinition", request.Kind);
        definition.Change(request.Name, request.Description, ImageUrl.FromOptional(request.ImageUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return BadgeViews.Definition(definition);
    }
}
