using Data.Model;

namespace Api.Model;

public record RegisterUserRequest(string Username, string Name, string Email, string Password)
{
    public User ToUser()
    {
        return new User(Username, Name, Email, Password);
    }
}
