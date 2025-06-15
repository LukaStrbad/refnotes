using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Controllers.Base;
using Server.Model;
using Server.Services;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FileController : GroupPermissionControllerBase
{
    private readonly IFileService _fileService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPublicFileService _publicFileService;

    public FileController(
        IFileService fileService,
        IFileStorageService fileStorageService,
        IGroupPermissionService groupPermissionService,
        IUserService userService,
        IPublicFileService publicFileService) : base(groupPermissionService, userService)
    {
        _fileService = fileService;
        _fileStorageService = fileStorageService;
        _publicFileService = publicFileService;
    }

    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFile(string directoryPath, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var files = Request.Form.Files;
        foreach (var file in files)
        {
            var name = file.FileName;
            var fileName = await _fileService.AddFile(directoryPath, name, groupId);
            await _fileStorageService.SaveFileAsync(fileName, file.OpenReadStream());
        }

        return Ok();
    }

    [HttpPost("addTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddTextFile(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        using var sr = new StreamReader(Request.Body);
        var content = await sr.ReadToEndAsync();
        var fileName = await _fileService.AddFile(directoryPath, name, groupId);
        await _fileStorageService.SaveFileAsync(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return Ok();
    }

    [HttpPost("moveFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MoveFile(string oldName, string newName, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        await _fileService.MoveFile(oldName, newName, groupId);
        return Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        var stream = _fileStorageService.GetFile(fileName);
        return File(stream, FileUtils.GetContentType(name), name);
    }

    [AllowAnonymous]
    [HttpGet("public/getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPublicFile(string urlHash)
    {
        var encryptedFile = await _publicFileService.GetEncryptedFileAsync(urlHash);
        if (encryptedFile is null || !await _publicFileService.IsPublicFileActive(urlHash))
            return NotFound("File not found.");

        var fileInfo = await _fileService.GetFileInfoAsync(encryptedFile.Id);
        if (fileInfo is null)
            return NotFound("File not found.");

        var stream = _fileStorageService.GetFile(encryptedFile.FilesystemName);
        return File(stream, FileUtils.GetContentType(fileInfo.Path), Path.GetFileName(fileInfo.Path));
    }

    [HttpGet("getImage")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetImage(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        try
        {
            var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
            if (fileName is null)
            {
                return File([], "application/octet-stream");
            }

            var stream = _fileStorageService.GetFile(fileName);

            const string contentType = "application/octet-stream";
            return File(stream, contentType, name);
        }
        catch (DirectoryNotFoundException)
        {
            // As this is an image, return 200 anyway not to clutter the console with errors while the user is inputting the image path
            return File([], "application/octet-stream");
        }
    }

    [HttpPost("saveTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SaveTextFile(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        await _fileStorageService.SaveFileAsync(fileName, Request.Body);
        await _fileService.UpdateTimestamp(directoryPath, name, groupId);
        return Ok();
    }

    [HttpDelete("deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile(string directoryPath, string name, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        try
        {
            await _fileService.DeleteFile(directoryPath, name, groupId);
            await _fileStorageService.DeleteFile(fileName);
        }
        catch (FileNotFoundException)
        {
            // This should never happen, but if it does, return 500
            // Return 500
            return StatusCode(500);
        }

        return Ok();
    }

    [HttpGet("getFileInfo")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetFileInfo(string filePath, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var fileInfo = await _fileService.GetFileInfo(filePath, groupId);
        return Ok(fileInfo);
    }

    [AllowAnonymous]
    [HttpGet("public/getFileInfo")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPublicFileInfo(string urlHash)
    {
        var encryptedFile = await _publicFileService.GetEncryptedFileAsync(urlHash);
        if (encryptedFile is null || !await _publicFileService.IsPublicFileActive(urlHash))
            return NotFound("File not found.");

        var fileInfo = await _fileService.GetFileInfoAsync(encryptedFile.Id);
        if (fileInfo is null)
            return NotFound("File not found.");

        return Ok(fileInfo);
    }

    [HttpGet("downloadFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DownloadFile(string path, int? groupId)
    {
        if (await GroupAccessForbidden(groupId))
            return Forbid();

        var (directoryPath, name) = FileUtils.SplitDirAndFile(path);
        var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        new FileExtensionContentTypeProvider().TryGetContentType(path, out var contentType);
        var stream = _fileStorageService.GetFile(fileName);
        return File(stream, contentType ?? "application/octet-stream", name);
    }
}