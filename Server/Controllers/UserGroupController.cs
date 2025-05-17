using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UserGroupController(IUserGroupService userGroupService) : ControllerBase
{
    [HttpPost("create")]
    [ProducesResponseType<GroupDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> Create(string? name = null)
    {
        var newGroup = await userGroupService.Create(name);
        return Ok(newGroup);
    }

    [HttpPost("{groupId:int}/update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(int groupId, [FromBody] UpdateGroupDto updateGroup)
    {
        await userGroupService.Update(groupId, updateGroup);
        return Ok();
    }

    [HttpGet("getUserGroups")]
    [ProducesResponseType<IEnumerable<GroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUserGroups()
    {
        var groups = await userGroupService.GetUserGroups();
        return Ok(groups);
    }

    [HttpGet("{groupId:int}/members")]
    [ProducesResponseType<IEnumerable<GroupUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetGroupMembers(int groupId)
    {
        var members = await userGroupService.GetGroupMembers(groupId);
        return Ok(members);
    }

    [HttpPost("{groupId:int}/assignRole")]
    public async Task<ActionResult> AssignRole(int groupId, [FromBody] AssignRoleDto assignRole)
    {
        try
        {
            await userGroupService.AssignRole(groupId, assignRole.UserId, assignRole.Role);
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
        try
        {
            await userGroupService.RemoveUser(groupId, userId);
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
        expiryTime ??= DateTime.UtcNow.AddDays(7);

        var code = await userGroupService.GenerateGroupAccessCode(groupId, (DateTime)expiryTime);
        return Ok(code);
    }

    [HttpPost("{groupId:int}/addCurrentUserWithCode")]
    public async Task<ActionResult> AddCurrentUserWithCode(int groupId, [FromBody] string accessCode)
    {
        try
        {
            await userGroupService.AddCurrentUserToGroup(groupId, accessCode);
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