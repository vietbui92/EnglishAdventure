namespace EnglishAdventure.Application.Database.Commands.ApplyMigrations;

public sealed record ApplyMigrationsResponse(IReadOnlyList<string> AppliedMigrations);
