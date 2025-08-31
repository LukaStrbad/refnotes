using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Model;

[Table("encrypted_directories")]
[Index(nameof(OwnerId))]
[Index(nameof(GroupId))]
public class EncryptedDirectory
{
    public EncryptedDirectory(string path, string pathHash, User owner)
    {
        Id = 0;
        Path = path;
        PathHash = pathHash;
        Files = [];
        SharedFiles = [];
        Directories = [];
        Owner = owner;
        OwnerId = owner.Id;
    }

    public EncryptedDirectory(string path, string pathHash, UserGroup group)
    {
        Id = 0;
        Path = path;
        PathHash = pathHash;
        Files = [];
        SharedFiles = [];
        Directories = [];
        Group = group;
    }

    public EncryptedDirectory()
    {
        Id = 0;
        Path = "";
        PathHash = "";
        Files = [];
        SharedFiles = [];
        Directories = [];
    }

    public int Id { get; init; }
    [StringLength(1024)] public string Path { get; init; }
    [StringLength(256)] public string PathHash { get; init; }
    public List<EncryptedFile> Files { get; init; }
    public List<SharedFile> SharedFiles { get; init; }
    public List<EncryptedDirectory> Directories { get; init; }
    public User? Owner { get; init; }
    public int? OwnerId { get; init; }
    public EncryptedDirectory? Parent { get; init; }
    public UserGroup? Group { get; init; }
    public int? GroupId { get; init; }
}
