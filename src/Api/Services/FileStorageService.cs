using Api.Streams;

namespace Api.Services;

public interface IFileStorageService
{
    Task SaveFileAsync(string fileName, Stream inputStream);
    Stream GetFile(string fileName);
    Task DeleteFile(string fileName);
    Task<long> GetFileSize(string fileName);
}

public class FileStorageService(IEncryptionService encryptionService, AppConfiguration appConfig) : IFileStorageService
{
    private static readonly Dictionary<string, SemaphoreSlim> FileLocks = new();
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

    private static SemaphoreSlim GetFileLock(string fileName)
    {
        lock (FileLocks)
        {
            if (FileLocks.TryGetValue(fileName, out var fileLock))
                return fileLock;

            fileLock = new SemaphoreSlim(1);
            FileLocks.Add(fileName, fileLock);
            return fileLock;
        }
    }

    public async Task SaveFileAsync(string fileName, Stream inputStream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.");
        }

        var fileLock = GetFileLock(fileName);
        if (!await fileLock.WaitAsync(LockTimeout))
            throw new TimeoutException("File lock timeout.");

        try
        {
            var filePath = Path.Combine(appConfig.DataDir, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await encryptionService.EncryptAesToStreamAsync(inputStream, stream);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public Stream GetFile(string fileName)
    {
        var fileLock = GetFileLock(fileName);

        if (!fileLock.Wait(LockTimeout))
            throw new TimeoutException("File lock timeout.");

        LockReleasingStream? lockReleasingStream = null;

        try
        {
            var filePath = Path.Combine(appConfig.DataDir, fileName);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var decryptedStream = encryptionService.DecryptAesToStream(stream);

            lockReleasingStream = new LockReleasingStream(decryptedStream, fileLock);
            return lockReleasingStream;
        }
        catch
        {
            // Manually release the lock as the stream is not initialized
            if (lockReleasingStream is null)
                fileLock.Release();
            else
                lockReleasingStream.Dispose();

            throw;
        }
    }

    public async Task DeleteFile(string fileName)
    {
        var fileLock = GetFileLock(fileName);

        if (!await fileLock.WaitAsync(LockTimeout))
            throw new TimeoutException("File lock timeout.");

        try
        {
            var filePath = Path.Combine(appConfig.DataDir, fileName);
            File.Delete(filePath);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<long> GetFileSize(string fileName)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File '{fileName}' not found.");
        }

        var fileLock = GetFileLock(fileName);

        if (!await fileLock.WaitAsync(LockTimeout))
            throw new TimeoutException("File lock timeout.");

        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        finally
        {
            fileLock.Release();
        }
    }
}
