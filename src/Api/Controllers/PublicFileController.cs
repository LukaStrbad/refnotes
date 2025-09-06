using Api.Controllers.Base;
using Api.Services;
using Api.Services.Files;
using Api.Utils;
using Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class PublicFileController : GroupPermissionControllerBase
{
    private readonly IPublicFileService _publicFileService;
    private readonly IFileService _fileService;
    private readonly ILogger<PublicFileController> _logger;

    public PublicFileController(
        IPublicFileService publicFileService,
        IFileService fileService,
        IGroupPermissionService groupPermissionService,
        IUserService userService,
        ILogger<PublicFileController> logger) : base(groupPermissionService, userService)
    {
        _publicFileService = publicFileService;
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet("getUrlHash")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetUrlHash(string filePath, int? groupId)
    {
        var file = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (file is null)
            return NotFound($"File '{filePath}' not found");

        var urlHash = await _publicFileService.GetUrlHashAsync(file.Id);
        return Ok(urlHash);
    }

    [HttpPost("create")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreatePublicFile(string filePath, int? groupId)
    {
        if (await GetGroupAccess(groupId, UserGroupRoleType.Admin) == GroupAccessStatus.AccessDenied)
        {
            _logger.LogWarning("User {username} tried to create a public file at path {path}", User.Identity?.Name,
                StringSanitizer.SanitizeLog(filePath));
            return Forbid();
        }

        var file = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (file is null)
            return NotFound($"File '{filePath}' not found");

        var urlHash = await _publicFileService.CreatePublicFileAsync(file.Id);
        return Ok(urlHash);
    }

    [HttpDelete("delete")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeletePublicFile(string filePath, int? groupId)
    {
        if (await GetGroupAccess(groupId, UserGroupRoleType.Admin) == GroupAccessStatus.AccessDenied)
        {
            _logger.LogWarning("User {username} tried to delete a public file at path {path}", User.Identity?.Name,
                StringSanitizer.SanitizeLog(filePath));
            return Forbid();
        }

        var file = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (file is null)
            return NotFound($"File '{filePath}' not found");

        var result = await _publicFileService.DeactivatePublicFileAsync(file.Id);
        return Ok(result);
    }
}
