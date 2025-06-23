using Data.Db.Model;
using Server.Extensions;

namespace Server.Services;

public class GroupPermissionService : IGroupPermissionService
{
    private readonly IUserGroupService _userGroupService;

    public GroupPermissionService(IUserGroupService userGroupService)
    {
        _userGroupService = userGroupService;
    }

    public async Task<bool> HasGroupAccessAsync(User user, int groupId)
    {
        var groupRole = await _userGroupService.GetGroupRoleTypeAsync(groupId, user.Id);
        return groupRole is not null;
    }

    public async Task<bool> HasGroupAccessAsync(User user, int groupId, UserGroupRoleType minRole)
    {
        var groupRole = await _userGroupService.GetGroupRoleTypeAsync(groupId, user.Id);
        if (groupRole is null)
            return false;

        return groupRole.GetRoleStrength() >= minRole.GetRoleStrength();
    }

    public async Task<bool> CanManageRoleAsync(User user, int groupId, UserGroupRoleType role)
    {
        if (role == UserGroupRoleType.Owner)
            return false;
        
        var groupRole = await _userGroupService.GetGroupRoleTypeAsync(groupId, user.Id);
        if (groupRole is null)
            return false;

        return groupRole.GetRoleStrength() > role.GetRoleStrength();
    }

    public async Task<bool> CanManageUserAsync(User user, int groupId, int userId)
    {
        var managingGroupRole = await _userGroupService.GetGroupRoleTypeAsync(groupId, user.Id);
        if (managingGroupRole is null)
            return false;

        var userGroupRole = await _userGroupService.GetGroupRoleTypeAsync(groupId, userId);
        return userGroupRole switch
        {
            null or UserGroupRoleType.Owner => false,
            _ => managingGroupRole.GetRoleStrength() > userGroupRole.GetRoleStrength()
        };
    }
}