using Api.Exceptions;
using Api.Model;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IAdminService
{
    Task<List<string>> ModifyRoles(ModifyRolesRequest modifyRolesRequest);
    Task<List<ResponseUser>> ListUsers();
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

    public async Task<List<ResponseUser>> ListUsers()
    {
        return await context.Users.Select(u => new ResponseUser(u)).ToListAsync();
    }
}
