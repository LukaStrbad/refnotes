using System.Text.Json;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;

namespace Api.Tests.ServiceTests;

public sealed class FileSyncServiceTests
{
    private readonly CancellationTokenSource _cts = new();
    private readonly FileSyncService _service;
    private readonly ISubscriber _subscriber;

    public FileSyncServiceTests()
    {
        var serviceProvider = new ServiceFixture<FileSyncService>().WithRedis().CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<FileSyncService>();
        _subscriber = serviceProvider.GetRequiredService<ISubscriber>();
    }

    [Fact]
    public async Task SendSyncSignalAsync_SendsSignal()
    {
        const int fileId = 123;
        var lastModified = DateTime.Now;
        var clientId = Guid.NewGuid().ToString();
        var channelMessage = new FileSyncChannelMessage(lastModified, clientId);
        var channelMessageJson = JsonSerializer.Serialize(channelMessage);

        await _service.SendSyncSignalAsync(fileId, channelMessage, _cts.Token);

        await _subscriber.Received(1).PublishAsync(
            Arg.Is<RedisChannel>(c => c.ToString().EndsWith(fileId.ToString())),
            Arg.Is<RedisValue>(channelMessageJson),
            CommandFlags.FireAndForget);
    }

    [Fact]
    public async Task SubscribeToSyncSignalAsync_GetsValues()
    {
        // Arrange
        const int fileId = 456;
        var testTime = DateTime.Now;
        var clientId = Guid.NewGuid().ToString();
        var channelMessage = new FileSyncChannelMessage(testTime, clientId);
        var channelMessageJson = JsonSerializer.Serialize(channelMessage);
        var channelName = $"FileSync-pub-{fileId}";

        var redisChannel = RedisChannel.Literal(channelName);

        Action<RedisChannel, RedisValue>? capturedHandler = null;

        _subscriber.SubscribeAsync(
            Arg.Do<RedisChannel>(ch => Assert.Equal(redisChannel, ch)),
            Arg.Do<Action<RedisChannel, RedisValue>>(cb => capturedHandler = cb),
            CommandFlags.FireAndForget
        ).Returns(Task.CompletedTask);

        FileSyncChannelMessage? receivedMessage = null;
        var tcs = new TaskCompletionSource();

        // Act
        await _service.SubscribeToSyncSignalAsync(fileId, message =>
        {
            receivedMessage = message;
            tcs.SetResult();
            return Task.CompletedTask;
        }, _cts.Token);

        // Simulate a message from Redis
        capturedHandler?.Invoke(redisChannel, channelMessageJson);

        // Wait for the callback to complete
        await tcs.Task;

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(testTime, receivedMessage.UpdatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(clientId, receivedMessage.ClientId);
    }
}
