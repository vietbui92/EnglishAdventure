using EnglishAdventure.Application.Database;
using EnglishAdventure.Application.Database.Commands.ApplyMigrations;

namespace EnglishAdventure.Application.UnitTests.Database;

public sealed class ApplyMigrationsCommandHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_migrations_the_migrator_applied()
    {
        var migrator = new FakeDatabaseMigrator(["20260101000000_First", "20260102000000_Second"]);
        var handler = new ApplyMigrationsCommandHandler(migrator);

        var response = await handler.Handle(new ApplyMigrationsCommand(), CancellationToken.None);

        Assert.Equal(1, migrator.Calls);
        Assert.Equal(["20260101000000_First", "20260102000000_Second"], response.AppliedMigrations);
    }

    [Fact]
    public async Task Handle_returns_an_empty_list_when_the_schema_is_up_to_date()
    {
        var handler = new ApplyMigrationsCommandHandler(new FakeDatabaseMigrator([]));

        var response = await handler.Handle(new ApplyMigrationsCommand(), CancellationToken.None);

        Assert.Empty(response.AppliedMigrations);
    }

    private sealed class FakeDatabaseMigrator : IDatabaseMigrator
    {
        private readonly IReadOnlyList<string> _applied;

        public FakeDatabaseMigrator(IReadOnlyList<string> applied)
        {
            _applied = applied;
        }

        public int Calls { get; private set; }

        public Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_applied);
        }
    }
}
