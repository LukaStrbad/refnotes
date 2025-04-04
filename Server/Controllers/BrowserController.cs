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
    public async Task<ActionResult<DirectoryDto>> List(string path)
    {
        var directory = await browserService.List(path);
        if (directory is null)
        {
            return NotFound("Directory not found.");
        }

        return Ok(directory);
    }

    [HttpPost("addDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AddDirectory(string path)
    {
        try
        {
            await browserService.AddDirectory(path);
            return Ok();
        }
        catch (DirectoryAlreadyExists e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("deleteDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDirectory(string path)
    {
        try
        {
            await browserService.DeleteDirectory(path);
            return Ok();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (DirectoryNotEmptyException e)
        {
            return BadRequest(e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}