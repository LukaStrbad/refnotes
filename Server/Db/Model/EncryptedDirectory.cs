using System.ComponentModel.DataAnnotations.Schema;
using Server.Model;
using Server.Services;

namespace Server.Db.Model;

[Table("encrypted_directories")]
public class EncryptedDirectory
{
    public EncryptedDirectory(string path, User owner)
    {
        Id = 0;
        Path = path;
        Files = [];
        Directories = [];
        Owner = owner;
    }
    
    public EncryptedDirectory()
    {
        Id = 0;
        Path = "";
        Files = [];
        Directories = [];
        Owner = null!;
    }

    public int Id { get; init; }
    public string Path { get; init; }
    public List<EncryptedFile> Files { get; init; }
    public List<EncryptedDirectory> Directories { get; init; }
    public User Owner { get; init; }
    public EncryptedDirectory? Parent { get; init; }

    public async Task<DirectoryDto> Decrypt(IEncryptionService encryptionService, IFileStorageService fileStorageService)
    {
        var name = DecryptedName(encryptionService);
        var filesTasks = Files.Select(async file =>
        {
            var fileName = file.DecryptedName(encryptionService);
            var tags = file.Tags.Select(tag => tag.DecryptedName(encryptionService));
            var size = await fileStorageService.GetFileSize(file.FilesystemName);
            return new FileDto(fileName, tags, size, file.Created, file.Modified);
        });
        var files = await Task.WhenAll(filesTasks);
        
        var directoriesTasks = Directories.Select(async directory => (await directory.Decrypt(encryptionService, fileStorageService)).Name);
        var directories = await Task.WhenAll(directoriesTasks);
        
        return new DirectoryDto(name, files, directories);
    }
    
    public string DecryptedPath(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Path);
    }

    public string DecryptedName(IEncryptionService encryptionService)
    {
        var path = DecryptedPath(encryptionService);
        return path == "/" ? "/" : System.IO.Path.GetFileName(path);
    }
}