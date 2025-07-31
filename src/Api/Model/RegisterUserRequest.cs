using System.ComponentModel.DataAnnotations;
using Data.Model;

namespace Api.Model;

public record RegisterUserRequest(
    [Required]
    string Username,
    [Required]
    string Name,
    [Required, EmailAddress]
    string Email,
    [Required, MinLength(8)]
    string Password)
{
    public User ToUser()
    {
        return new User(Username, Name, Email, Password);
    }
}
