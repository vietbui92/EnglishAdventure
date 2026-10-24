using EnglishAdventure.Application.Learning.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetAdventure;

/// <summary>The adventure map of the current child session, with locks and best stars (S3.4, S4.2).</summary>
public sealed record GetAdventureQuery : IRequest<AdventureMap>;
