using System.Diagnostics;
using Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;

namespace MigrationService;

public class MigrationWorker : BackgroundService
{
    public const string ActivitySourceName = "Migrations";

    private readonly ILogger<MigrationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly SchedulerMigrations _schedulerMigrations;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public MigrationWorker(
        ILogger<MigrationWorker> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        SchedulerMigrations schedulerMigrations)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _schedulerMigrations = schedulerMigrations;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(ExecuteAsync), ActivityKind.Client);
        
        try
        {
            _logger.LogInformation("Migrating scheduler database");
            await _schedulerMigrations.Migrate(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while migrating scheduler database");
            activity?.AddException(e);
            throw;
        }

        try
        {
            _logger.LogInformation("Migrating main database");
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RefNotesContext>();

            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while running migration");
            activity?.AddException(e);
            throw;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private async Task RunMigrationAsync(RefNotesContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
        _logger.LogInformation("Migration completed");
    }
}
