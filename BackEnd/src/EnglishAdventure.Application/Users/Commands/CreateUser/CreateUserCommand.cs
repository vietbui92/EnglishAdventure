using MediatR;

namespace EnglishAdventure.Application.Users.Commands.CreateUser;

/// <summary>
/// Creates an Admin or Parent user. A Parent user gets the Parent role; an Admin user starts without roles,
/// which are given separately (they need <c>roles.manage</c>). Child users wait for OQ-001.
/// </summary>
public sealed record CreateUserCommand(string Email, string Password, string UserType) : IRequest<Guid>
{
    // Keeps the password out of logs and exception messages that print the request.
    public override string ToString() =>
        $"{nameof(CreateUserCommand)} {{ Email = {Email}, UserType = {UserType} }}";
}
