using EnglishAdventure.Application.Family.Models;
using MediatR;

namespace EnglishAdventure.Application.Family.Queries.GetChildProfile;

/// <summary>One of the signed-in Parent's profiles; another family's profile is reported as not found.</summary>
public sealed record GetChildProfileQuery(Guid ProfileId) : IRequest<ChildProfileSummary>;
