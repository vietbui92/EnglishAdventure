namespace EnglishAdventure.Domain.Exceptions;

/// <summary>
/// Base type for domain rule violations. Application and API layers map this to appropriate HTTP responses.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    /// <param name="code">A stable code a client can react to, for example one of <see cref="ContentErrorCodes"/>.</param>
    public DomainException(string message, string? code)
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Returned as the ProblemDetails "code" extension; null when the rule has none.</summary>
    public string? Code { get; }
}
