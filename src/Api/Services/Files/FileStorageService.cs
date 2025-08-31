using System.Buffers;
using Api.Services.Redis;
using Api.Streams;
using StackExchange.Redis;

namespace Api.Services.Files;

public interface IFileStorageService
{
    Task SaveFileAsync(string fileName, Stream inputStream);
    Stream GetFile(string fileName);
    Task DeleteFile(string fileName);
    Task<long> GetFileSize(string fileName);
}

public class FileStorageService(
    IEncryptionService encryptionService,
    IConnectionMultiplexer muxer,
    AppSettings appSettings,
    IRedisLockProvider lockProvider) : IFileStorageService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

    private static string GetFileLockKey(string fileName) => $"FileLock:{fileName}";
    private static RedisKey GetFileSizeKey(string fileName) => new($"FileSize:{fileName}");

    public async Task SaveFileAsync(string fileName, Stream inputStream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.");
        }

        await using var handle = await lockProvider.TryAcquireWriteLockAsync(GetFileLockKey(fileName), LockTimeout);
        if (handle is null)
            throw new TimeoutException("File lock timeout.");

        var filePath = Path.Combine(appSettings.DataDir, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await encryptionService.EncryptAesToStreamAsync(inputStream, stream);

        var redis = muxer.GetDatabase();
        var sizeKey = GetFileSizeKey(fileName);
        try
        {
            // Try to set the file size from the input stream position
            await redis.StringSetAsync(sizeKey, inputStream.Position);
        }
        catch (Exception)
        {
            // If the input stream position is not available, we need to delete the size key
            await redis.KeyDeleteAsync(sizeKey);
        }
    }

    public Stream GetFile(string fileName)
    {
        RedisLockReleasingStream? lockReleasingStream = null;
        // No "using" as the lock should be released manually or by the lock releasing stream
        var handle = lockProvider.TryAcquireReadLock(GetFileLockKey(fileName), LockTimeout);
        if (handle is null)
            throw new TimeoutException("File lock timeout.");

        try
        {
            var filePath = Path.Combine(appSettings.DataDir, fileName);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var decryptedStream = encryptionService.DecryptAesToStream(stream);

            lockReleasingStream = new RedisLockReleasingStream(decryptedStream, handle);
            return lockReleasingStream;
        }
        catch
        {
            // Manually release the lock as the stream is not initialized
            if (lockReleasingStream is null)
                handle.Dispose();
            else
                lockReleasingStream.Dispose();

            throw;
        }
    }

    public async Task DeleteFile(string fileName)
    {
        var redis = muxer.GetDatabase();

        await using (var handle = await lockProvider.TryAcquireWriteLockAsync(GetFileLockKey(fileName), LockTimeout))
        {
            if (handle is null)
                throw new TimeoutException("File lock timeout.");

            var filePath = Path.Combine(appSettings.DataDir, fileName);
            File.Delete(filePath);
        }

        // Clear the file size cache after deleting a file
        var sizeKey = GetFileSizeKey(fileName);
        await redis.KeyDeleteAsync(sizeKey);
    }

    public async Task<long> GetFileSize(string fileName)
    {
        var filePath = Path.Combine(appSettings.DataDir, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File '{fileName}' not found.");
        }

        var sizeKey = GetFileSizeKey(fileName);
        var redis = muxer.GetDatabase();

        // Try to get the file size from Redis cache
        var cachedSize = await redis.StringGetAsync(sizeKey);
        if (cachedSize.HasValue && cachedSize.TryParse(out long sizeLong))
        {
            return sizeLong;
        }

        await using var handle = await lockProvider.TryAcquireReadLockAsync(GetFileLockKey(fileName), LockTimeout);

        if (handle is null)
            throw new TimeoutException("File lock timeout.");

        // No "using" as the file stream will be disposed by the stream returned by DecryptAesToStream
        var fileStream = File.OpenRead(filePath);
        sizeLong = 0;

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        // Read the file size by decrypting the stream
        await using (var decryptedStream = encryptionService.DecryptAesToStream(fileStream))
        {
            var readBytes = await decryptedStream.ReadAsync(buffer);
            while (readBytes > 0)
            {
                sizeLong += readBytes;
                readBytes = await decryptedStream.ReadAsync(buffer);
            }
        }

        // Store size to cache
        await redis.StringSetAsync(sizeKey, sizeLong);

        return sizeLong;
    }
}
