namespace EnglishAdventure.Api.Contracts.Rewards;

/// <param name="ImageUrl">An https picture; blank or null to let the app draw the badge.</param>
public sealed record BadgeDefinitionRequest(string Name, string Description, string? ImageUrl);
