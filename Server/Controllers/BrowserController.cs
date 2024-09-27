using Microsoft.AspNetCore.Mvc;
using Server.Db;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class BrowserController : ControllerBase
{
    private readonly IBrowserServiceRepository _browserServiceRepository;
    private readonly IFileService _fileService;

    public BrowserController(IBrowserServiceRepository browserServiceRepository, IFileService fileService)
    {
        _browserServiceRepository = browserServiceRepository;
        _fileService = fileService;
    }

    [HttpGet("list")]
    [ProducesResponseType<ResponseDirectory>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> List(string path)
    {
        var directory = await _browserServiceRepository.List(User, path);
        if (directory is null)
        {
            return NotFound("Directory not found.");
        }

        return Ok(directory);
    }

    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddFile(string directoryPath, string name, [FromBody] IFormFile file)
    {
        var fileName = await _browserServiceRepository.AddFile(User, directoryPath, name);
        await _fileService.SaveFile(fileName, file.OpenReadStream());

        return Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name)
    {
        var fileName = await _browserServiceRepository.GetFilesystemFilePath(User, directoryPath, name);
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
}