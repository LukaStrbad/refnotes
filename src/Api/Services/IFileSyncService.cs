using Api.Model;

namespace Api.Services;

public interface IFileSyncService
{
    Task SendSyncSignalAsync(int fileId, FileSyncChannelMessage channelMessage, CancellationToken cancellationToken);
    Task SubscribeToSyncSignalAsync(int fileId, Func<FileSyncChannelMessage, Task> callback, CancellationToken cancellationToken);
}
