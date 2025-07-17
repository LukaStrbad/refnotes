using Api.Model;
using Data.Model;

namespace Api.Services;

public interface IUserService
{
    /// <summary>
    /// Gets currently logged-in user
    /// </summary>
    Task<User> GetCurrentUser();

    Task<User> GetByUsername(string username);

    Task<User> EditUser(int userId, EditUserRequest details);
    
    Task UnconfirmEmail(int userId);
}
