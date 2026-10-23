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

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
