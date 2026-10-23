using MediatR;

namespace EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;

/// <summary>
/// Foundation example query — verifies the CQRS pipeline, not product functionality.
/// </summary>
public sealed record GetFoundationStatusQuery(string? Echo = null) : IRequest<GetFoundationStatusResponse>;
