namespace EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;

public sealed record GetFoundationStatusResponse(
    string Message,
    bool IsFoundationExample,
    string? Echo);
