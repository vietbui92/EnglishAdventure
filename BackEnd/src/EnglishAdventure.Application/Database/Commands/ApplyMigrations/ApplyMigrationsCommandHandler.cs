using MediatR;

namespace EnglishAdventure.Application.Database.Commands.ApplyMigrations;

public sealed class ApplyMigrationsCommandHandler : IRequestHandler<ApplyMigrationsCommand, ApplyMigrationsResponse>
{
    private readonly IDatabaseMigrator _databaseMigrator;

    public ApplyMigrationsCommandHandler(IDatabaseMigrator databaseMigrator)
    {
        _databaseMigrator = databaseMigrator;
    }

    public async Task<ApplyMigrationsResponse> Handle(
        ApplyMigrationsCommand request,
        CancellationToken cancellationToken)
    {
        var appliedMigrations = await _databaseMigrator.ApplyPendingMigrationsAsync(cancellationToken);
        return new ApplyMigrationsResponse(appliedMigrations);
    }
}
