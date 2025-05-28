using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UserGroupController : ControllerBase
{
    private readonly IUserGroupService _userGroupService;
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly IUserService _userService;

    public UserGroupController(
        IUserGroupService userGroupService,
        IGroupPermissionService groupPermissionService,
        IUserService userService)
    {
        _userGroupService = userGroupService;
        _groupPermissionService = groupPermissionService;
        _userService = userService;
    }

    [HttpPost("create")]
    [ProducesResponseType<GroupDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> Create(string? name = null)
    {
        var newGroup = await _userGroupService.Create(name);
        return Ok(newGroup);
    }

    [HttpPost("{groupId:int}/update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(int groupId, [FromBody] UpdateGroupDto updateGroup)
    {
        // Only Admins and Owners can update groups
        if (!await _groupPermissionService.HasGroupAccessAsync(await _userService.GetUser(), groupId,
                UserGroupRoleType.Admin))
            return Forbid();

        await _userGroupService.Update(groupId, updateGroup);
        return Ok();
    }

    [HttpGet("getUserGroups")]
    [ProducesResponseType<IEnumerable<GroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUserGroups()
    {
        var groups = await _userGroupService.GetUserGroups();
        return Ok(groups);
    }

    [HttpGet("{groupId:int}/members")]
    [ProducesResponseType<IEnumerable<GroupUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetGroupMembers(int groupId)
    {
        if (!await _groupPermissionService.HasGroupAccessAsync(await _userService.GetUser(), groupId))
            return Forbid();

        var members = await _userGroupService.GetGroupMembers(groupId);
        return Ok(members);
    }

    [HttpPost("{groupId:int}/assignRole")]
    public async Task<ActionResult> AssignRole(int groupId, [FromBody] AssignRoleDto assignRole)
    {
        if (!await _groupPermissionService.CanManageRoleAsync(await _userService.GetUser(), groupId, assignRole.Role))
            return Forbid();

        try
        {
            await _userGroupService.AssignRole(groupId, assignRole.UserId, assignRole.Role);
            return Ok();
        }
        catch (Exception e) when (e is InvalidOperationException or UserIsOwnerException)
        {
            return BadRequest(e.Message);
        }
        catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("{groupId:int}/removeUser")]
    public async Task<ActionResult> RemoveUser(int groupId, [FromQuery] int userId)
    {
        if (!await _groupPermissionService.CanManageUserAsync(await _userService.GetUser(), groupId, userId))
            return Forbid();

        try
        {
            await _userGroupService.RemoveUser(groupId, userId);
            return Ok();
        }
        catch (Exception e) when (e is InvalidOperationException or UserIsOwnerException)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{groupId:int}/generateAccessCode")]
    public async Task<ActionResult> GenerateAccessCode(int groupId, [FromBody] DateTime? expiryTime = null)
    {
        // Only Admins and Owners can generate access codes
        if (!await _groupPermissionService.HasGroupAccessAsync(await _userService.GetUser(), groupId,
                UserGroupRoleType.Admin))
            return Forbid();

        expiryTime ??= DateTime.UtcNow.AddDays(7);

        var code = await _userGroupService.GenerateGroupAccessCode(groupId, (DateTime)expiryTime);
        return Ok(code);
    }

    [HttpPost("{groupId:int}/addCurrentUserWithCode")]
    public async Task<ActionResult> AddCurrentUserWithCode(int groupId, [FromBody] string accessCode)
    {
        try
        {
            await _userGroupService.AddCurrentUserToGroup(groupId, accessCode);
            return Ok();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
        catch (AccessCodeInvalidException e)
        {
            return Forbid(e.Message);
        }
    }
}