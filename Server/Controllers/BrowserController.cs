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
public class BrowserController : ControllerBase
{
    private readonly IBrowserService _browserService;
    private readonly IFileService _fileService;

    public BrowserController(IBrowserService browserService, IFileService fileService)
    {
        _browserService = browserService;
        _fileService = fileService;
    }

    [HttpGet("list")]
    [ProducesResponseType<ResponseDirectory>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> List(string path)
    {
        var directory = await _browserService.List(User, path);
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
            await _browserService.AddDirectory(User, path);
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
            await _browserService.DeleteDirectory(User, path);
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

    private async Task<ActionResult?> AddFileResult(string directoryPath, string name, Stream stream)
    {
        try
        {
            var fileName = await _browserService.AddFile(User, directoryPath, name);
            await _fileService.SaveFileAsync(fileName, stream);
            return null;
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (FileAlreadyExistsException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddFile(string directoryPath)
    {
        var files = Request.Form.Files;
        foreach (var file in files)
        {
            var name = file.FileName;
            var result = await AddFileResult(directoryPath, name, file.OpenReadStream());
            if (result is not null)
            {
                return result;
            }
        }

        return Ok();
    }

    [HttpPost("addTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddTextFile(string directoryPath, string name)
    {
        using var sr = new StreamReader(Request.Body);
        var content = await sr.ReadToEndAsync();
        var result = await AddFileResult(directoryPath, name, new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return result ?? Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name)
    {
        try
        {
            var fileName = await _browserService.GetFilesystemFilePath(User, directoryPath, name);
            if (fileName is null)
            {
                return NotFound("File not found.");
            }

            var stream = _fileService.GetFile(fileName);

            var contentType = name.EndsWith(".md") || name.EndsWith(".markdown")
                ? "text/markdown"
                : "application/octet-stream";
            return File(stream, contentType, name);
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("getImage")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetImage(string directoryPath, string name)
    {
        try
        {
            var fileName = await _browserService.GetFilesystemFilePath(User, directoryPath, name);
            if (fileName is null)
            {
                return File([], "application/octet-stream");
            }

            var stream = _fileService.GetFile(fileName);

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
        try
        {
            var fileName = await _browserService.GetFilesystemFilePath(User, directoryPath, name);
            if (fileName is null)
            {
                return NotFound("File not found.");
            }

            await _fileService.SaveFileAsync(fileName, Request.Body);
            return Ok();
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpDelete("deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile(string directoryPath, string name)
    {
        var fileName = await _browserService.GetFilesystemFilePath(User, directoryPath, name);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        try
        {
            await _browserService.DeleteFile(User, directoryPath, name);
            await _fileService.DeleteFile(fileName);
        }
        catch (FileNotFoundException)
        {
            // This should never happen, but if it does, return 500
            // Return 500
            return StatusCode(500);
        }

        return Ok();
    }
}
