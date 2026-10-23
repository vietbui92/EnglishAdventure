namespace EnglishAdventure.Api.IntegrationTests;

/// <summary>
/// A fact that runs only against a real SQL Server. Without <see cref="ConnectionStringVariable"/> it is reported
/// as skipped rather than passing without verifying anything.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "ENGLISH_ADVENTURE_TEST_SQL_CONNECTION";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to run SQL Server integration tests.";
        }
    }
}
