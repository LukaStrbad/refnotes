namespace Server.Services;

public class FileService(IEncryptionService encryptionService, AppConfiguration appConfig) : IFileService
{
    public async Task SaveFile(string fileName, Stream inputStream)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        encryptionService.EncryptAesToStream(inputStream, stream);
    }
    
    public Stream GetFile(string fileName)
    {
        var filePath = Path.Combine(appConfig.DataDir, fileName);
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var decryptedStream = new MemoryStream();
        encryptionService.DecryptAesToStream(stream, decryptedStream);
        decryptedStream.Position = 0;
        return decryptedStream;
    }
}