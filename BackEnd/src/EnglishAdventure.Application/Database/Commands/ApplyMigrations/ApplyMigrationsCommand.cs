using MediatR;

namespace EnglishAdventure.Application.Database.Commands.ApplyMigrations;

/// <summary>
/// Development tooling: brings the database schema up to date. Not exposed outside Development.
/// </summary>
public sealed record ApplyMigrationsCommand : IRequest<ApplyMigrationsResponse>;
