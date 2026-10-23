using EnglishAdventure.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Api.IntegrationTests.Database;

/// <summary>
/// The migrations must produce exactly the schema the current model describes: same tables, columns, keys, indexes,
/// foreign keys and check constraints, with the same names. Guards hand-written migrations such as
/// RenameAccountsToUsers.
/// </summary>
public sealed class MigrationSchemaSqlServerTests
{
    private const string DatabaseNamePrefix = "EnglishAdventure_SchemaTests_";

    // Catalog names use the server collation; COLLATE DATABASE_DEFAULT lets them be concatenated with literals.
    private const string SchemaQuery = """
        SELECT 'column ' + t.name COLLATE DATABASE_DEFAULT + '.' + c.name COLLATE DATABASE_DEFAULT
               + ' ' + ty.name COLLATE DATABASE_DEFAULT + '(' + CAST(c.max_length AS varchar(10)) + ')'
               + CASE WHEN c.is_nullable = 1 THEN ' null' ELSE ' not null' END AS Value
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE t.name <> '__EFMigrationsHistory'
        UNION ALL
        SELECT 'index ' + t.name COLLATE DATABASE_DEFAULT + '.' + i.name COLLATE DATABASE_DEFAULT
               + CASE WHEN i.is_primary_key = 1 THEN ' primary' WHEN i.is_unique = 1 THEN ' unique' ELSE '' END
               + ' (' + STRING_AGG(c.name COLLATE DATABASE_DEFAULT, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) + ')'
               + ISNULL(' where ' + i.filter_definition COLLATE DATABASE_DEFAULT, '')
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE t.name <> '__EFMigrationsHistory' AND i.name IS NOT NULL
        GROUP BY t.name, i.name, i.is_primary_key, i.is_unique, i.filter_definition
        UNION ALL
        SELECT 'foreign key ' + fk.name COLLATE DATABASE_DEFAULT
               + ' ' + OBJECT_NAME(fk.parent_object_id) COLLATE DATABASE_DEFAULT
               + ' -> ' + OBJECT_NAME(fk.referenced_object_id) COLLATE DATABASE_DEFAULT
               + ' on delete ' + fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT
        FROM sys.foreign_keys fk
        UNION ALL
        SELECT 'check ' + cc.name COLLATE DATABASE_DEFAULT + ' ' + cc.definition COLLATE DATABASE_DEFAULT
        FROM sys.check_constraints cc
        """;

    [SqlServerFact]
    public async Task Migrations_produce_the_same_schema_as_the_current_model()
    {
        var migratedName = DatabaseNamePrefix + "migrated_" + Guid.NewGuid().ToString("N");
        var modelName = DatabaseNamePrefix + "model_" + Guid.NewGuid().ToString("N");

        await using var migrated = CreateContext(migratedName);
        await using var model = CreateContext(modelName);

        try
        {
            await migrated.Database.MigrateAsync();
            await model.Database.EnsureCreatedAsync();

            var migratedSchema = await ReadSchemaAsync(migrated);
            var modelSchema = await ReadSchemaAsync(model);

            Assert.NotEmpty(modelSchema);
            Assert.Equal(modelSchema, migratedSchema);
        }
        finally
        {
            await migrated.Database.EnsureDeletedAsync();
            await model.Database.EnsureDeletedAsync();
        }
    }

    private static EnglishAdventureDbContext CreateContext(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionStringVariable))
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<EnglishAdventureDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new EnglishAdventureDbContext(options);
    }

    private static async Task<List<string>> ReadSchemaAsync(EnglishAdventureDbContext context)
    {
        var rows = await context.Database.SqlQueryRaw<string>(SchemaQuery).ToListAsync();
        return rows.Order(StringComparer.Ordinal).ToList();
    }
}
