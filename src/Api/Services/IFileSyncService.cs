namespace Api.Services;

public interface IFileSyncService
{
    Task SendSyncSignalAsync(int fileId, DateTimeOffset lastModified, CancellationToken cancellationToken);
    Task SubscribeToSyncSignalAsync(int fileId, Func<DateTime, Task> callback, CancellationToken cancellationToken);
}
