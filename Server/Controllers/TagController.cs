using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Controllers.Base;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TagController : GroupPermissionControllerBase
{
    private readonly ITagService _tagService;

    public TagController(
        ITagService tagService,
        IGroupPermissionService groupPermissionService,
        IUserService userService) : base(groupPermissionService, userService)
    {
        _tagService = tagService;
    }

    [HttpGet("listAllTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListAllTags()
    {
        return Ok(await _tagService.ListAllTags());
    }

    [HttpGet("listAllGroupTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListAllGroupTags(int groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        return Ok(await _tagService.ListAllGroupTags(groupId));
    }

    [HttpGet("listFileTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListFileTags(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        return Ok(await _tagService.ListFileTags(directoryPath, name, groupId));
    }

    [HttpPost("addFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFileTag(string directoryPath, string name, string tag, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        await _tagService.AddFileTag(directoryPath, name, tag, groupId);
        return Ok();
    }

    [HttpDelete("removeFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RemoveFileTag(string directoryPath, string name, string tag, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        await _tagService.RemoveFileTag(directoryPath, name, tag, groupId);
        return Ok();
    }
}