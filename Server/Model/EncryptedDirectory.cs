using Server.Services;

namespace Server.Model;

public class EncryptedDirectory
{
    public int Id { get; init; }
    public required string FilesystemName { get; init; }
    public required string Path { get; init; }
    public required List<EncryptedFile> Files { get; init; }
    public required List<EncryptedDirectory> Directories { get; init; }
    public required User Owner { get; init; }

    public ResponseDirectory Decrypt(IEncryptionService encryptionService)
    {
        var name = DecryptedName(encryptionService);
        var files = Files.Select(file => file.DecryptedName(encryptionService)).ToList();
        var directories = Directories.Select(directory => directory.Decrypt(encryptionService).Name).ToList();
        return new ResponseDirectory
        {
            Name = name,
            Files = files,
            Directories = directories
        };
    }
    
    public string DecryptedPath(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Path);
    }

    public string DecryptedName(IEncryptionService encryptionService)
    {
        var path = DecryptedPath(encryptionService);
        return System.IO.Path.GetDirectoryName(path) ?? path;
    }
}