using StackExchange.Redis;

namespace Api.Services;

public sealed class FileSyncService : IFileSyncService
{
    private readonly IConnectionMultiplexer _muxer;
    private readonly ILogger<FileSyncService> _logger;

    public FileSyncService(IConnectionMultiplexer muxer, ILogger<FileSyncService> logger)
    {
        _muxer = muxer;
        _logger = logger;
    }

    private static RedisChannel CreateChannel(int fileId)
    {
        var channelName = $"FileSync-pub-{fileId}";
        return RedisChannel.Literal(channelName);
    }

    public async Task SendSyncSignalAsync(int fileId, DateTimeOffset lastModified, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending sync signal for file {FileId} at {LastModified}", fileId, lastModified);
        var channel = CreateChannel(fileId);
        var subscriber = _muxer.GetSubscriber();
        await subscriber.PublishAsync(channel, lastModified.ToUnixTimeMilliseconds(), CommandFlags.FireAndForget);
    }

    public async Task SubscribeToSyncSignalAsync(int fileId, Func<DateTime, Task> callback, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscribing to sync signal for file {FileId}", fileId);
        var channel = CreateChannel(fileId);
        var subscriber = _muxer.GetSubscriber();
        
        Action<RedisChannel, RedisValue> handler = (_, message) =>
        {
            if (!message.HasValue || !long.TryParse(message, out var unixMillis)) return;

            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis);
            var dateTime = dateTimeOffset.DateTime;
            Task.Run(() => callback(dateTime), cancellationToken); 
        };
        
        await subscriber.SubscribeAsync(channel, handler, CommandFlags.FireAndForget);
        
        // Unsubscribe on cancellation
        cancellationToken.Register(() => subscriber.Unsubscribe(channel, handler));
    }
}
