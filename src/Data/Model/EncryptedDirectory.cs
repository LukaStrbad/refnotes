using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Model;

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
    [StringLength(1024)] public string Path { get; init; }
    public List<EncryptedFile> Files { get; init; }
    public List<EncryptedDirectory> Directories { get; init; }
    public User? Owner { get; init; }
    public int? OwnerId { get; init; }
    public EncryptedDirectory? Parent { get; init; }
    public UserGroup? Group { get; init; }
    public int? GroupId { get; init; }
}
