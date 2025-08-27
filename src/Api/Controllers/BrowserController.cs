using Api.Controllers.Base;
using Api.Model;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class BrowserController : GroupPermissionControllerBase
{
    private readonly IDirectoryService _directoryService;

    public BrowserController(
        IDirectoryService directoryService,
        IGroupPermissionService groupPermissionService,
        IUserService userService) : base(groupPermissionService, userService)
    {
        _directoryService = directoryService;
    }

    [HttpGet("list")]
    [ProducesResponseType<DirectoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> List(string path, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var directory = await _directoryService.List(groupId, path);
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
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        await _directoryService.AddDirectory(path, groupId);
        return Ok();
    }

    [HttpDelete("deleteDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteDirectory(string path, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        try
        {
            await _directoryService.DeleteDirectory(path, groupId);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}
