using MySqlConnector;

namespace MigrationService;

public class SchedulerMigrations
{
    private const string MigrationsDir = "Sql/Migrations";
    private const string MigrationsTableSql = "Sql/migrations_table.sql";

    private readonly MySqlDataSource _dataSource;
    private readonly ILogger<SchedulerMigrations> _logger;

    public SchedulerMigrations(
        [FromKeyedServices("scheduler")] MySqlDataSource dataSource,
        ILogger<SchedulerMigrations> logger)
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
        try
        {
            await using var cmd = new MySqlCommand("SHOW TABLES LIKE 'migrations'", connection);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            var tableExists = result != null;

            // If the table doesn't exist, create it
            if (!tableExists)
            {
                _logger.LogInformation("Creating migrations table");
                var sqlContent = await File.ReadAllTextAsync(MigrationsTableSql, cancellationToken);
                await using var createTableCmd = new MySqlCommand(sqlContent, connection);
                await createTableCmd.ExecuteNonQueryAsync(cancellationToken);

                // Return early as there are no migrations
                return [];
            }

            // Get all migrations
            _logger.LogInformation("Reading migrations from database");
            var migrations = new List<string>();
            await using var migrationsCmd = new MySqlCommand("SELECT name FROM migrations ORDER BY id", connection);
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

    private async Task ApplyMigration(string migrationName, MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying migration {MigrationName}", migrationName);
        var migrationSql = await GetMigrationSql(migrationName);
        await using var cmd = new MySqlCommand(migrationSql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Add migration to the database
        await using var insertMigrationCmd = new MySqlCommand(
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
