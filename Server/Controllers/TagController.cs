﻿using Microsoft.AspNetCore.Authorization;
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
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<string>>> ListFileTags(string directoryPath, string name)
    {
        try
        {
            return Ok(await tagService.ListFileTags(directoryPath, name));
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Directory not found.");
        }
    }

    [HttpPost("addFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddFileTag(string directoryPath, string name, string tag)
    {
        try
        {
            await tagService.AddFileTag(directoryPath, name, tag);
            return Ok();
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Directory not found.");
        }
    }

    [HttpDelete("removeFileTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFileTag(string directoryPath, string name, string tag)
    {
        try
        {
            await tagService.RemoveFileTag(directoryPath, name, tag);
            return Ok();
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("Directory not found.");
        }
    }
}