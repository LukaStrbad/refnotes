namespace Server.Services;

public interface IFileStorageService
{
    Task SaveFileAsync(string fileName, Stream inputStream);
    Stream GetFile(string fileName);
    Task DeleteFile(string fileName);
    Task<long> GetFileSize(string fileName);
}

public class FileStorageService(IEncryptionService encryptionService, AppConfiguration appConfig) : IFileStorageService
{
    public async Task SaveFileAsync(string fileName, Stream inputStream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.");
        }

        var filePath = Path.Combine(appConfig.DataDir, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await encryptionService.EncryptAesToStreamAsync(inputStream, stream);
    }

    public Stream GetFile(string fileName)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var decryptedStream = new MemoryStream();
        encryptionService.DecryptAesToStream(stream, decryptedStream);
        decryptedStream.Position = 0;
        return decryptedStream;
    }

    public Task DeleteFile(string fileName)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<long> GetFileSize(string fileName)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File '{fileName}' not found.");
        }
        
        var fileInfo = new FileInfo(filePath);
        return Task.FromResult(fileInfo.Length);
    }
}
