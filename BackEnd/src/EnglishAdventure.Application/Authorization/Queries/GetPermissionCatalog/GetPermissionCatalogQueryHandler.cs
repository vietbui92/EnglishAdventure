using MediatR;

namespace EnglishAdventure.Application.Authorization.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionDefinition>>
{
    public Task<IReadOnlyList<PermissionDefinition>> Handle(
        GetPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Permissions.All);
    }
}
