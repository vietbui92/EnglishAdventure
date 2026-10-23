using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.RestoreChildProfile;

public sealed class RestoreChildProfileCommandHandler : IRequestHandler<RestoreChildProfileCommand>
{
    private readonly IChildProfileRepository _profiles;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreChildProfileCommandHandler(
        IChildProfileRepository profiles,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _profiles = profiles;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RestoreChildProfileCommand request, CancellationToken cancellationToken)
    {
        var parentId = FamilyRules.CurrentParentId(_userContext);
        var profile = await _profiles.GetOwnedAsync(request.ProfileId, parentId, cancellationToken)
            ?? throw new NotFoundException("ChildProfile", request.ProfileId);
        if (profile.IsActive)
        {
            return;
        }

        await FamilyRules.EnsureNicknameFreeAsync(_profiles, parentId, profile.Nickname, profile.Id, cancellationToken);
        profile.Restore(await _profiles.CountActiveAsync(parentId, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
