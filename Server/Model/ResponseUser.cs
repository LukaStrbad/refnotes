using Server.Db.Model;

namespace Server.Model;

public class ResponseUser(User user)
{
    public int Id { get; init; } = user.Id;
    public string Username { get; init; } = user.Username;
    public string Name { get; init; } = user.Name;
    public string Email { get; init; } = user.Email;
    public string[] Roles { get; set; } = user.Roles;
}