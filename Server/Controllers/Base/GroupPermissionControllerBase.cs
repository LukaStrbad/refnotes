using Microsoft.AspNetCore.Mvc;
using Data.Db.Model;
using Server.Services;

namespace Server.Controllers.Base;

public abstract class GroupPermissionControllerBase : ControllerBase
{
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly IUserService _userService;

    protected GroupPermissionControllerBase(IGroupPermissionService groupPermissionService, IUserService userService)
    {
        _groupPermissionService = groupPermissionService;
        _userService = userService;
    }

    protected async Task<bool> GroupAccessForbidden(int? groupId)
    {
        if (groupId is null)
            return false;

        return !await _groupPermissionService.HasGroupAccessAsync(await _userService.GetUser(), (int)groupId);
    }
    
    protected async Task<bool> GroupAccessForbidden(int? groupId, UserGroupRoleType minRole)
    {
        if (groupId is null)
            return false;
        
        return !await _groupPermissionService.HasGroupAccessAsync(await _userService.GetUser(), (int)groupId, minRole);
    }
}
