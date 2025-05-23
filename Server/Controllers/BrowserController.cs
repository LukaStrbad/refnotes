using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class BrowserController(IBrowserService browserService) : ControllerBase
{
    [HttpGet("list")]
    [ProducesResponseType<DirectoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DirectoryDto>> List(string path, int? groupId)
    {
        var directory = await browserService.List(groupId, path);
        if (directory is null)
        {
            return NotFound("Directory not found.");
        }

        return Ok(directory);
    }

    [HttpPost("addDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddDirectory(string path, int? groupId)
    {
        await browserService.AddDirectory(path, groupId);
        return Ok();
    }

    [HttpDelete("deleteDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteDirectory(string path, int? groupId)
    {
        try
        {
            await browserService.DeleteDirectory(path, groupId);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}