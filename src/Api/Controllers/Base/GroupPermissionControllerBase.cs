using Api.Services;
using Data.Model;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Base;

public abstract class GroupPermissionControllerBase : ControllerBase
{
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly IUserService _userService;

    protected GroupPermissionControllerBase(IGroupPermissionService groupPermissionService, IUserService userService)
    {
        _groupPermissionService = groupPermissionService;
        _userService = userService;
    }

    protected async Task<GroupAccessStatus> GetGroupAccess(int? groupId)
    {
        if (groupId is null)
            return GroupAccessStatus.NoGroup;

        var hasAccess = await _groupPermissionService.HasGroupAccessAsync(await _userService.GetCurrentUser(), (int)groupId);
        return hasAccess ? GroupAccessStatus.AccessGranted : GroupAccessStatus.AccessDenied;
    }

    protected async Task<GroupAccessStatus> GetGroupAccess(int? groupId, UserGroupRoleType minRole)
    {
        if (groupId is null)
            return GroupAccessStatus.NoGroup;

        var hasAccess =
            await _groupPermissionService.HasGroupAccessAsync(await _userService.GetCurrentUser(), (int)groupId, minRole);
        return hasAccess ? GroupAccessStatus.AccessGranted : GroupAccessStatus.AccessDenied;
    }

    protected enum GroupAccessStatus
    {
        NoGroup,
        AccessDenied,
        AccessGranted
    }
}
