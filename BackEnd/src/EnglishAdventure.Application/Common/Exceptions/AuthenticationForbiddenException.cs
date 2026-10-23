namespace EnglishAdventure.Application.Common.Exceptions;

public sealed class AuthenticationForbiddenException : Exception
{
    public AuthenticationForbiddenException(string message)
        : base(message)
    {
    }
}
