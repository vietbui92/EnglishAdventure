namespace EnglishAdventure.Application.Common.Exceptions;

/// <summary>
/// The credentials are right, but the user may not sign in. <see cref="Code"/> tells the client why, so it can react
/// (for example offer a new confirmation link); it is returned as the ProblemDetails "code" extension.
/// </summary>
public sealed class AuthenticationForbiddenException : Exception
{
    public const string UserInactiveCode = "user-inactive";
    public const string EmailNotConfirmedCode = "email-not-confirmed";

    public AuthenticationForbiddenException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
