using MediatR;

namespace EnglishAdventure.Application.Authorization.Queries.GetPermissionCatalog;

public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionDefinition>>;
