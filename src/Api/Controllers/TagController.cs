using Api.Controllers.Base;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

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
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> ListAllTags()
    {
        return Ok(await _tagService.ListAllTags());
    }

    [HttpGet("listAllGroupTags")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> ListAllGroupTags(int groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        return Ok(await _tagService.ListAllGroupTags(groupId));
    }

    [HttpGet("listFileTags")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult> ListFileTags(string directoryPath, string name, int? groupId)
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
