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
    [ProducesResponseType<ResponseDirectory>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> List(string path)
    {
        var directory = await browserService.List(User, path);
        if (directory is null)
        {
            return NotFound("Directory not found.");
        }

        return Ok(directory);
    }

    [HttpPost("addDirectory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddDirectory(string path)
    {
        try
        {
            await browserService.AddDirectory(User, path);
            return Ok();
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
            await browserService.DeleteDirectory(User, path);
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
