using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.ArchiveChildProfile;

public sealed class ArchiveChildProfileCommandHandler : IRequestHandler<ArchiveChildProfileCommand>
{
    private readonly IChildProfileRepository _profiles;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ArchiveChildProfileCommandHandler(
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

    public async Task Handle(ArchiveChildProfileCommand request, CancellationToken cancellationToken)
    {
        var parentId = FamilyRules.CurrentParentId(_userContext);
        var profile = await _profiles.GetOwnedAsync(request.ProfileId, parentId, cancellationToken)
            ?? throw new NotFoundException("ChildProfile", request.ProfileId);

        profile.Archive(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
