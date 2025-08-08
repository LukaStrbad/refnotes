using Medallion.Threading;

namespace Api.Services.Redis;

public interface IRedisLockProvider
{
    Task<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(
        string resourceName,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);

    Task<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(
        string resourceName,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);
    
    IDistributedSynchronizationHandle? TryAcquireReadLock(
        string resourceName,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);
}
