using MySqlConnector;
using Npgsql;

namespace MigrationService;

public class PostgresSchedulerMigrations : ISchedulerMigrations
{
    private const string MigrationsDir = "Sql/Postgres/Migrations";
    private const string MigrationsTableSql = "Sql/Postgres/migrations_table.sql";

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresSchedulerMigrations> _logger;

    public PostgresSchedulerMigrations(
        [FromKeyedServices("scheduler")] NpgsqlDataSource dataSource,
        ILogger<PostgresSchedulerMigrations> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task Migrate(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migrating database");
        var migratedFiles = await GetMigratedFiles(cancellationToken);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var migrationFile in GetMigrationFiles())
            {
                var migrationName = Path.GetFileName(migrationFile);
                if (migratedFiles.Contains(migrationName))
                {
                    _logger.LogInformation("Migration {MigrationName} already applied", migrationName);
                    continue;
                }

                await ApplyMigration(migrationName, connection, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error while migrating database");
            throw;
        }
    }

    private async Task<List<string>> GetMigratedFiles(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_dataSource.ConnectionString);
        var searchPath = connectionStringBuilder.SearchPath ?? "public";
        try
        {
            await using var cmd = new NpgsqlCommand("""
                                                    SELECT EXISTS (SELECT 1 FROM information_schema.tables
                                                        WHERE table_name = 'migrations'
                                                        and table_schema = @searchPath
                                                    ) AS table_existence;
                                                    """, connection);
            cmd.Parameters.AddWithValue("@searchPath", searchPath);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            var tableExists = result is true;

            // If the table doesn't exist, create it
            if (!tableExists)
            {
                _logger.LogInformation("Creating migrations table");
                var sqlContent = await File.ReadAllTextAsync(MigrationsTableSql, cancellationToken);
                await using var createTableCmd = new NpgsqlCommand(sqlContent, connection);
                await createTableCmd.ExecuteNonQueryAsync(cancellationToken);

                // Return early as there are no migrations
                return [];
            }

            // Get all migrations
            _logger.LogInformation("Reading migrations from database");
            var migrations = new List<string>();
            await using var migrationsCmd = new NpgsqlCommand("SELECT name FROM migrations ORDER BY id", connection);
            await using var reader = await migrationsCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var migrationName = reader.GetString(0);
                migrations.Add(migrationName);
            }

            return migrations;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error while reading migrations from database");
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task ApplyMigration(string migrationName, NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying migration {MigrationName}", migrationName);
        var migrationSql = await GetMigrationSql(migrationName);
        await using var cmd = new NpgsqlCommand(migrationSql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Add migration to the database
        await using var insertMigrationCmd = new NpgsqlCommand(
            "INSERT INTO migrations (name) VALUES (@name)", connection);
        insertMigrationCmd.Parameters.AddWithValue("@name", migrationName);
        await insertMigrationCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<string> GetMigrationFiles() =>
        Directory.GetFiles(MigrationsDir, "*.sql").Select(Path.GetFullPath);

    private static async Task<string> GetMigrationSql(string migrationName)
    {
        var migrationPath = Path.Combine(MigrationsDir, migrationName);
        return await File.ReadAllTextAsync(migrationPath);
    }
}
