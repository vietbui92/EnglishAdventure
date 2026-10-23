using EnglishAdventure.Application.Authentication.Models;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserProfile>;
