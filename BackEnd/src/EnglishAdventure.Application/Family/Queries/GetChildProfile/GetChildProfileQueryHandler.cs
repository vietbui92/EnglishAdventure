using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Family.Models;
using MediatR;

namespace EnglishAdventure.Application.Family.Queries.GetChildProfile;

public sealed class GetChildProfileQueryHandler : IRequestHandler<GetChildProfileQuery, ChildProfileSummary>
{
    private readonly IChildProfileQueries _queries;
    private readonly IUserContext _userContext;

    public GetChildProfileQueryHandler(IChildProfileQueries queries, IUserContext userContext)
    {
        _queries = queries;
        _userContext = userContext;
    }

    public async Task<ChildProfileSummary> Handle(GetChildProfileQuery request, CancellationToken cancellationToken) =>
        await _queries.GetAsync(request.ProfileId, FamilyRules.CurrentParentId(_userContext), cancellationToken)
        ?? throw new NotFoundException("ChildProfile", request.ProfileId);
}
