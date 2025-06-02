using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Services;

namespace Server.Db.Model;

[Table("encrypted_directories")]
[Index(nameof(OwnerId))]
[Index(nameof(GroupId))]
public class EncryptedDirectory
{
    public EncryptedDirectory(string path, User owner)
    {
        Id = 0;
        Path = path;
        Files = [];
        Directories = [];
        Owner = owner;
        OwnerId = owner.Id;
    }

    public EncryptedDirectory(string path, UserGroup group)
    {
        Id = 0;
        Path = path;
        Files = [];
        Directories = [];
        Group = group;
    }
    
    public EncryptedDirectory()
    {
        Id = 0;
        Path = "";
        Files = [];
        Directories = [];
    }

    public int Id { get; init; }
    [StringLength(1024)]
    public string Path { get; init; }
    public List<EncryptedFile> Files { get; init; }
    public List<EncryptedDirectory> Directories { get; init; }
    public User? Owner { get; init; }
    public int? OwnerId { get; init; }
    public EncryptedDirectory? Parent { get; init; }
    public UserGroup? Group { get; init; }
    public int? GroupId { get; init; }

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