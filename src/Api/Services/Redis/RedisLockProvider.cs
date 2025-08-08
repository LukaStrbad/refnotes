using Medallion.Threading;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace Api.Services.Redis;

public sealed class RedisLockProvider : IRedisLockProvider
{
    private readonly IConnectionMultiplexer _muxer;

    public RedisLockProvider(IConnectionMultiplexer muxer)
    {
        _muxer = muxer;
    }

    public async Task<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(
        string resourceName,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var redis = _muxer.GetDatabase();
        var redisLock = new RedisDistributedReaderWriterLock(resourceName, redis);
        return await redisLock.TryAcquireWriteLockAsync(timeout, cancellationToken);
    }

    public async Task<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(
        string resourceName,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var redis = _muxer.GetDatabase();
        var redisLock = new RedisDistributedReaderWriterLock(resourceName, redis);
        return await redisLock.TryAcquireReadLockAsync(timeout, cancellationToken);
    }

    public IDistributedSynchronizationHandle? TryAcquireReadLock(string resourceName, TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var redis = _muxer.GetDatabase();
        var redisLock = new RedisDistributedReaderWriterLock(resourceName, redis);
        return redisLock.TryAcquireReadLock(timeout, cancellationToken);
    }
}
