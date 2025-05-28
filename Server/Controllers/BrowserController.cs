using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Controllers.Base;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class BrowserController : GroupPermissionControllerBase
{
    private readonly IBrowserService _browserService;

    public BrowserController(
        IBrowserService browserService,
        IGroupPermissionService groupPermissionService,
        IUserService userService) : base(groupPermissionService, userService)
    {
        _browserService = browserService;
    }

    [HttpGet("list")]
    [ProducesResponseType<DirectoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DirectoryDto>> List(string path, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var directory = await _browserService.List(groupId, path);
        if (directory is null)
        {
            return NotFound("Directory not found.");
        }

        return Ok(directory);
    }

    [HttpPost("addDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> AddDirectory(string path, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        await _browserService.AddDirectory(path, groupId);
        return Ok();
    }

    [HttpDelete("deleteDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteDirectory(string path, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        try
        {
            await _browserService.DeleteDirectory(path, groupId);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}