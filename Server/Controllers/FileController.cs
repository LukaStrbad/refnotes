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
public class FileController(IFileService fileService, IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFile(string directoryPath)
    {
        var files = Request.Form.Files;
        foreach (var file in files)
        {
            var name = file.FileName;
            var fileName = await fileService.AddFile(directoryPath, name);
            await fileStorageService.SaveFileAsync(fileName, file.OpenReadStream());
        }

        return Ok();
    }

    [HttpPost("addTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddTextFile(string directoryPath, string name)
    {
        using var sr = new StreamReader(Request.Body);
        var content = await sr.ReadToEndAsync();
        var fileName = await fileService.AddFile(directoryPath, name);
        await fileStorageService.SaveFileAsync(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return Ok();
    }

    [HttpPost("moveFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MoveFile(string oldName, string newName)
    {
        await fileService.MoveFile(oldName, newName);
        return Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name)
    {
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name);
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
    public async Task<ActionResult> GetImage(string directoryPath, string name)
    {
        try
        {
            var fileName = await fileService.GetFilesystemFilePath(directoryPath, name);
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
    public async Task<ActionResult> SaveTextFile(string directoryPath, string name)
    {
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        await fileStorageService.SaveFileAsync(fileName, Request.Body);
        await fileService.UpdateTimestamp(directoryPath, name);
        return Ok();
    }

    [HttpDelete("deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile(string directoryPath, string name)
    {
        var fileName = await fileService.GetFilesystemFilePath(directoryPath, name);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        try
        {
            await fileService.DeleteFile(directoryPath, name);
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
    public async Task<ActionResult<FileDto>> GetFileInfo(string filePath)
    {
        var fileInfo = await fileService.GetFileInfo(filePath);
        return Ok(fileInfo);
    }
}