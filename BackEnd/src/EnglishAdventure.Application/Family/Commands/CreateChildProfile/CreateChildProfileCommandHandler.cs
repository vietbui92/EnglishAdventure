using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.CreateChildProfile;

public sealed class CreateChildProfileCommandHandler : IRequestHandler<CreateChildProfileCommand, Guid>
{
    private readonly IChildProfileRepository _profiles;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateChildProfileCommandHandler(
        IChildProfileRepository profiles,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _profiles = profiles;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateChildProfileCommand request, CancellationToken cancellationToken)
    {
        var parentId = FamilyRules.CurrentParentId(_userContext);
        var nickname = Nickname.From(request.Nickname);
        await FamilyRules.EnsureNicknameFreeAsync(_profiles, parentId, nickname, exceptProfileId: null, cancellationToken);

        var profile = ChildProfile.Create(
            parentId,
            nickname,
            request.AvatarKey,
            await _profiles.CountActiveAsync(parentId, cancellationToken),
            _timeProvider.GetUtcNow().UtcDateTime);

        _profiles.Add(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }
}
