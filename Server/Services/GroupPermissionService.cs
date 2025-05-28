using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Extensions;
using Server.Utils;

namespace Server.Services;

public class GroupPermissionService : IGroupPermissionService
{
    private readonly RefNotesContext _context;

    public GroupPermissionService(RefNotesContext context)
    {
        _context = context;
    }

    public async Task<bool> HasGroupAccessAsync(User user, int groupId)
    {
        var groupRole = await GetUserGroupRoleAsync(groupId, user.Id);
        return groupRole is not null;
    }

    public async Task<bool> HasGroupAccessAsync(User user, int groupId, UserGroupRoleType minRole)
    {
        var groupRole = await GetUserGroupRoleAsync(groupId, user.Id);
        if (groupRole is null)
            return false;

        return groupRole.Role.GetRoleStrength() >= minRole.GetRoleStrength();
    }
    
    public async Task<bool> CanManageRoleAsync(User user, int groupId, UserGroupRoleType role)
    {
        var groupRole = await GetUserGroupRoleAsync(groupId, user.Id);
        if (groupRole is null)
            return false;
        
        return groupRole.Role.GetRoleStrength() > role.GetRoleStrength();
    }

    public async Task<bool> CanManageUserAsync(User user, int groupId, int userId)
    {
        var managingGroupRole = await GetUserGroupRoleAsync(groupId, user.Id);
        if (managingGroupRole is null)
            return false;
        
        var userGroupRole = await GetUserGroupRoleAsync(groupId, userId);
        if (userGroupRole is null)
            return false;
        
        return managingGroupRole.Role.GetRoleStrength() > userGroupRole.Role.GetRoleStrength();
    }

    private async Task<UserGroupRole?> GetUserGroupRoleAsync(int groupId, int userId)
    {
        return await _context.UserGroupRoles
            .Where(group => group.UserGroupId == groupId && group.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
