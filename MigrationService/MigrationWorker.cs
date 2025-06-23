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
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public MigrationWorker(ILogger<MigrationWorker> logger, IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(ExecuteAsync), ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RefNotesContext>();

            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while running migration");
            activity?.RecordException(e);
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