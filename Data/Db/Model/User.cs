using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Db.Model;

[Table("users")]
public class User(
    int id,
    string username,
    string name,
    string email,
    string password
)
{
    public int Id { get; init; } = id;

    [StringLength(256)] public string Username { get; init; } = username;

    [StringLength(256)] public string Name { get; init; } = name;

    [StringLength(1024)] public string Email { get; init; } = email;

    [StringLength(4096)] public string Password { get; set; } = password;

    public string[] Roles { get; set; } = [];
}
