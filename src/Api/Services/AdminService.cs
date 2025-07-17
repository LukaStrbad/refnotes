using Api.Exceptions;
using Api.Model;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IAdminService
{
    Task<List<string>> ModifyRoles(ModifyRolesRequest modifyRolesRequest);
    Task<List<UserResponse>> ListUsers();
}

public class AdminService(RefNotesContext context) : IAdminService
{
    public async Task<List<string>> ModifyRoles(ModifyRolesRequest modifyRolesRequest)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == modifyRolesRequest.Username);
        if (user is null)
        {
            throw new UserNotFoundException("User not found.");
        }

        var roles = user.Roles.ToList();
        roles.AddRange(modifyRolesRequest.AddRoles);
        roles = roles.Except(modifyRolesRequest.RemoveRoles).ToList();
        // Remove duplicates
        roles = roles.Distinct().ToList();

        user.Roles = roles.ToArray();
        await context.SaveChangesAsync();
        return roles;
    }

    public async Task<List<UserResponse>> ListUsers()
    {
        var users = await context.Users.ToListAsync();
        return users.Select(UserResponse.FromUser).ToList();
    }
}
