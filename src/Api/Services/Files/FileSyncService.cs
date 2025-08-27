using System.Text.Json;
using Api.Model;
using StackExchange.Redis;

namespace Api.Services.Files;

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

    public async Task SendSyncSignalAsync(int fileId, FileSyncChannelMessage channelMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending sync signal for file {FileId}", fileId);
        var channel = CreateChannel(fileId);
        var subscriber = _muxer.GetSubscriber();
        var json = JsonSerializer.Serialize(channelMessage);
        await subscriber.PublishAsync(channel, json, CommandFlags.FireAndForget);
    }

    public async Task SubscribeToSyncSignalAsync(int fileId, Func<FileSyncChannelMessage, Task> callback,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscribing to sync signal for file {FileId}", fileId);
        var channel = CreateChannel(fileId);
        var subscriber = _muxer.GetSubscriber();

        Action<RedisChannel, RedisValue> handler = (_, message) =>
        {
            if (!message.HasValue) return;

            if (JsonSerializer.Deserialize<FileSyncChannelMessage>(message.ToString()) is not { } channelMessage)
                return;

            Task.Run(() => callback(channelMessage), cancellationToken);
        };

        await subscriber.SubscribeAsync(channel, handler, CommandFlags.FireAndForget);

        // Unsubscribe on cancellation
        cancellationToken.Register(() => subscriber.Unsubscribe(channel, handler));
    }
}
