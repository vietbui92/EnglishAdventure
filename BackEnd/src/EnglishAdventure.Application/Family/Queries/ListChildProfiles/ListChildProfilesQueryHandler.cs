using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Family.Models;
using MediatR;

namespace EnglishAdventure.Application.Family.Queries.ListChildProfiles;

public sealed class ListChildProfilesQueryHandler
    : IRequestHandler<ListChildProfilesQuery, IReadOnlyList<ChildProfileSummary>>
{
    private readonly IChildProfileQueries _queries;
    private readonly IUserContext _userContext;

    public ListChildProfilesQueryHandler(IChildProfileQueries queries, IUserContext userContext)
    {
        _queries = queries;
        _userContext = userContext;
    }

    public Task<IReadOnlyList<ChildProfileSummary>> Handle(
        ListChildProfilesQuery request,
        CancellationToken cancellationToken) =>
        _queries.ListAsync(FamilyRules.CurrentParentId(_userContext), request.IncludeArchived, cancellationToken);
}
