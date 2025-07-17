using Data.Model;

namespace Api.Model;

public record UserResponse(int Id, string Username, string Name, string Email, string[] Roles, bool EmailConfirmed)
{
    public static UserResponse FromUser(User user)
        => new(user.Id, user.Username, user.Name, user.Email, user.Roles, user.EmailConfirmed);
}
