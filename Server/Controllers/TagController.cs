using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TagController(ITagService tagService) : ControllerBase
{
    [HttpGet("listAllTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListAllTags()
    {
        return Ok(await tagService.ListAllTags());
    }

    [HttpGet("listFileTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListFileTags(string directoryPath, string name)
    {
        return Ok(await tagService.ListFileTags(directoryPath, name));
    }

    [HttpPost("addFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFileTag(string directoryPath, string name, string tag)
    {
        await tagService.AddFileTag(directoryPath, name, tag);
        return Ok();
    }

    [HttpDelete("removeFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RemoveFileTag(string directoryPath, string name, string tag)
    {
        await tagService.RemoveFileTag(directoryPath, name, tag);
        return Ok();
    }
}