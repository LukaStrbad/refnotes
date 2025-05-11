using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Exceptions;
using Server.Model;
using Server.Services;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FileController(IFileService fileService, IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFile(string directoryPath, int? groupId)
    {
        var files = Request.Form.Files;
        foreach (var file in files)
        {
            var name = file.FileName;
            var fileName = await fileService.AddFile(directoryPath, name, groupId);
            await fileStorageService.SaveFileAsync(fileName, file.OpenReadStream());
        }

        return Ok();
    }

    [HttpPost("addTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddTextFile(string directoryPath, string name, int? groupId)
    {
        using var sr = new StreamReader(Request.Body);
        var content = await sr.ReadToEndAsync();
        var fileName = await fileService.AddFile(directoryPath, name, groupId);
        await fileStorageService.SaveFileAsync(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return Ok();
    }

    [HttpPost("moveFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MoveFile(string oldName, string newName, int? groupId)
    {
        await fileService.MoveFile(oldName, newName, groupId);
        return Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name, int? groupId)
    {
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        var stream = fileStorageService.GetFile(fileName);

        var contentType = name.EndsWith(".md") || name.EndsWith(".markdown")
            ? "text/markdown"
            : "application/octet-stream";
        return File(stream, contentType, name);
    }

    [HttpGet("getImage")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetImage(string directoryPath, string name, int? groupId)
    {
        try
        {
            var fileName = await fileService.GetFilesystemFilePath(directoryPath, name, groupId);
            if (fileName is null)
            {
                return File([], "application/octet-stream");
            }

            var stream = fileStorageService.GetFile(fileName);

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
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        await fileStorageService.SaveFileAsync(fileName, Request.Body);
        await fileService.UpdateTimestamp(directoryPath, name, groupId);
        return Ok();
    }

    [HttpDelete("deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile(string directoryPath, string name, int? groupId)
    {
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        try
        {
            await fileService.DeleteFile(directoryPath, name, groupId);
            await fileStorageService.DeleteFile(fileName);
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
    public async Task<ActionResult<FileDto>> GetFileInfo(string filePath, int? groupId)
    {
        var fileInfo = await fileService.GetFileInfo(filePath, groupId);
        return Ok(fileInfo);
    }

    [HttpGet("downloadFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DownloadFile(string path, int? groupId)
    {
        var (directoryPath, name) = ServiceUtils.SplitDirAndFile(path);
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        new FileExtensionContentTypeProvider().TryGetContentType(path, out var contentType);
        var stream = fileStorageService.GetFile(fileName);
        return File(stream, contentType ?? "application/octet-stream", name);
    }
}