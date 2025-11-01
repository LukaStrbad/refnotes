namespace MigrationService;

public interface ISchedulerMigrations
{
    Task Migrate(CancellationToken cancellationToken);
}
