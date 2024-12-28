namespace Server.Services;

public interface IFileService
{
    Task SaveFileAsync(string fileName, Stream inputStream);
    Stream GetFile(string fileName);
    Task DeleteFile(string fileName);
}
