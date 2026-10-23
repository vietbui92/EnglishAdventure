using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.UpdateChildProfile;

public sealed class UpdateChildProfileCommandHandler : IRequestHandler<UpdateChildProfileCommand>
{
    private readonly IChildProfileRepository _profiles;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateChildProfileCommandHandler(
        IChildProfileRepository profiles,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _profiles = profiles;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateChildProfileCommand request, CancellationToken cancellationToken)
    {
        var parentId = FamilyRules.CurrentParentId(_userContext);
        var profile = await _profiles.GetOwnedAsync(request.ProfileId, parentId, cancellationToken)
            ?? throw new NotFoundException("ChildProfile", request.ProfileId);

        var nickname = Nickname.From(request.Nickname);
        await FamilyRules.EnsureNicknameFreeAsync(_profiles, parentId, nickname, profile.Id, cancellationToken);

        profile.Update(nickname, request.AvatarKey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
