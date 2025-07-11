using Api.Services;
using Api.Tests.Data;
using NSubstitute;
using StackExchange.Redis;

namespace Api.Tests.ServiceTests;

public sealed class FileSyncServiceTests
{
    private readonly CancellationTokenSource _cts = new();

    [Theory, AutoData]
    public async Task SendSyncSignalAsync_SendsSignal(Sut<FileSyncService> sut, ISubscriber subscriber)
    {
        const int fileId = 123;
        const long unixMillis = 1640995200000;
        var lastModified = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis);

        await sut.Value.SendSyncSignalAsync(fileId, lastModified, _cts.Token);

        await subscriber.Received(1).PublishAsync(
            Arg.Is<RedisChannel>(c => c.ToString().EndsWith(fileId.ToString())),
            Arg.Is<RedisValue>(unixMillis),
            CommandFlags.FireAndForget);
    }

    [Theory, AutoData]
    public async Task SubscribeToSyncSignalAsync_GetsValues(Sut<FileSyncService> sut, ISubscriber subscriber)
    {
        // Arrange
        const int fileId = 456;
        var testTime = DateTimeOffset.UtcNow;
        var unixTime = testTime.ToUnixTimeMilliseconds();
        var channelName = $"FileSync-pub-{fileId}";

        var redisChannel = RedisChannel.Literal(channelName);

        Action<RedisChannel, RedisValue>? capturedHandler = null;

        subscriber.SubscribeAsync(
            Arg.Do<RedisChannel>(ch => Assert.Equal(redisChannel, ch)),
            Arg.Do<Action<RedisChannel, RedisValue>>(cb => capturedHandler = cb),
            CommandFlags.FireAndForget
        ).Returns(Task.CompletedTask);

        DateTime? received = null;
        var tcs = new TaskCompletionSource();
        
        // Act
        await sut.Value.SubscribeToSyncSignalAsync(fileId, dt =>
        {
            received = dt;
            tcs.SetResult();
            return Task.CompletedTask;
        }, _cts.Token);

        // Simulate a message from Redis
        capturedHandler?.Invoke(redisChannel, unixTime);
        
        // Wait for the callback to complete
        await tcs.Task;

        // Assert
        Assert.NotNull(received);
        Assert.Equal(testTime.UtcDateTime, received.Value, TimeSpan.FromMilliseconds(1));
    }
}
