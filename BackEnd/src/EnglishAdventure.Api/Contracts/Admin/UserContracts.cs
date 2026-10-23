namespace EnglishAdventure.Api.Contracts.Admin;

/// <param name="UserType">"Admin" or "Parent".</param>
public sealed record CreateUserRequest(string Email, string Password, string UserType);

/// <param name="Roles">Role names the user should have afterwards; an empty list removes all roles.</param>
public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);
