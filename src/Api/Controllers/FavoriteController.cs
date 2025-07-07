using Api.Controllers.Base;
using Api.Model;
using Api.Services;
using Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public sealed class FavoriteController : GroupPermissionControllerBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly IFileService _fileService;
    private readonly IFileServiceUtils _fileServiceUtils;

    public FavoriteController(
        IGroupPermissionService groupPermissionService,
        IUserService userService,
        IFavoriteService favoriteService,
        IFileService fileService,
        IFileServiceUtils fileServiceUtils) : base(
        groupPermissionService, userService)
    {
        _favoriteService = favoriteService;
        _fileService = fileService;
        _fileServiceUtils = fileServiceUtils;
    }

    [HttpPost("favoriteFile")]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> FavoriteFile(string filePath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var encryptedFile = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (encryptedFile is null)
            return NotFound($"File '{filePath}' not found");

        await _favoriteService.FavoriteFile(encryptedFile);
        return Ok();
    }

    [HttpPost("unfavoriteFile")]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UnfavoriteFile(string filePath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var encryptedFile = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (encryptedFile is null)
            return NotFound($"File '{filePath}' not found");

        await _favoriteService.UnfavoriteFile(encryptedFile);
        return Ok();
    }

    [HttpGet("getFavoriteFiles")]
    [ProducesResponseType<IEnumerable<FileFavoriteDetails>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetFavoriteFiles()
    {
        var favoriteFiles = await _favoriteService.GetFavoriteFiles();
        return Ok(favoriteFiles);
    }

    [HttpPost("favoriteDirectory")]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> FavoriteDirectory(string directoryPath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var directory = await _fileServiceUtils.GetDirectory(directoryPath, false, groupId);
        if (directory is null)
            return NotFound($"Directory '{directoryPath}' not found");

        await _favoriteService.FavoriteDirectory(directory);
        return Ok();
    }
    
    [HttpPost("unfavoriteDirectory")]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UnfavoriteDirectory(string directoryPath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();
        
        var directory = await _fileServiceUtils.GetDirectory(directoryPath, false, groupId);
        if (directory is null)
            return NotFound($"Directory '{directoryPath}' not found");
        
        await _favoriteService.UnfavoriteDirectory(directory);
        return Ok();
    }
    
    [HttpGet("getFavoriteDirectories")]
    [ProducesResponseType<IEnumerable<DirectoryFavoriteDetails>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetFavoriteDirectories()
    {
        var favoriteDirectories = await _favoriteService.GetFavoriteDirectories();
        return Ok(favoriteDirectories);
    }
}
