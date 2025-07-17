using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("users")]
public class User
{
    public User(string username,
        string name,
        string email,
        string password)
    {
        Username = username;
        Name = name;
        Email = email;
        Password = password;
    }

    public int Id { get; init; }

    [StringLength(256)] public string Username { get; init; }

    [StringLength(256)] public string Name { get; init; }

    [StringLength(1024)] public string Email { get; init; }

    [StringLength(4096)] public string Password { get; set; }

    public string[] Roles { get; set; } = [];
    public bool EmailConfirmed { get; set; } = false;
}
