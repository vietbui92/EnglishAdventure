using EnglishAdventure.Application.Users.Models;
using MediatR;

namespace EnglishAdventure.Application.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IRequest<UserSummary>;
