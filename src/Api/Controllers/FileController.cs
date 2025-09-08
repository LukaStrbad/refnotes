using System.Net.WebSockets;
using System.Text;
using Api.Controllers.Base;
using Api.Model;
using Api.Services;
using Api.Services.Files;
using Api.Services.Schedulers;
using Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FileController : GroupPermissionControllerBase
{
    private readonly IFileService _fileService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPublicFileService _publicFileService;
    private readonly IPublicFileScheduler _publicFileScheduler;
    private readonly ILogger<FileController> _logger;
    private readonly IFileSyncService _fileSyncService;
    private readonly IWebSocketFileSyncService _webSocketFileSyncService;
    private readonly IFileShareService _fileShareService;
    private readonly IFileServiceUtils _fileServiceUtils;
    private readonly IDirectoryService _directoryService;
    private readonly IUserService _userService;

    public FileController(
        IFileService fileService,
        IFileStorageService fileStorageService,
        IGroupPermissionService groupPermissionService,
        IUserService userService,
        IPublicFileService publicFileService,
        IPublicFileScheduler publicFileScheduler,
        ILogger<FileController> logger,
        IFileSyncService fileSyncService,
        IWebSocketFileSyncService webSocketFileSyncService,
        IFileShareService fileShareService,
        IFileServiceUtils fileServiceUtils,
        IDirectoryService directoryService) : base(groupPermissionService, userService)
    {
        _fileService = fileService;
        _fileStorageService = fileStorageService;
        _userService = userService;
        _publicFileService = publicFileService;
        _publicFileScheduler = publicFileScheduler;
        _logger = logger;
        _fileSyncService = fileSyncService;
        _webSocketFileSyncService = webSocketFileSyncService;
        _fileShareService = fileShareService;
        _fileServiceUtils = fileServiceUtils;
        _directoryService = directoryService;
    }

    [HttpPost("addFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AddFile(string directoryPath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
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
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
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
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        await _fileService.MoveFile(oldName, newName, groupId);
        return Ok();
    }

    [HttpGet("getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFile(string directoryPath, string name, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var fileName = await _fileService.GetFilesystemFilePath(directoryPath, name, groupId);
        if (fileName is null)
        {
            return NotFound("File not found.");
        }

        var stream = _fileStorageService.GetFile(fileName);
        return File(stream, FileUtils.GetContentType(name), name);
    }

    [HttpGet("shared/getFile")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetSharedFile(string path)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(path, user);
        if (sharedFile is null)
            return NotFound("File not found.");

        var encryptedFile = await _fileShareService.GetEncryptedFileFromSharedFile(sharedFile);
        var fileInfo = await _fileService.GetFileInfoAsync(encryptedFile.Id);
        if (fileInfo is null)
            return NotFound("File not found.");

        var stream = _fileStorageService.GetFile(encryptedFile.FilesystemName);
        return File(stream, FileUtils.GetContentType(fileInfo.Name), fileInfo.Name);
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
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
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

    [HttpGet("public/getImage")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPublicImage(string urlHash, string imagePath)
    {
        var encryptedFile = await _publicFileService.GetEncryptedFileAsync(urlHash);
        if (encryptedFile is null || !await _publicFileService.IsPublicFileActive(urlHash))
            return NotFound("File not found.");

        var image = await _fileService.GetEncryptedFileByRelativePathAsync(encryptedFile, imagePath);
        if (image is null)
            return BadRequest();

        if (!await _publicFileService.HasAccessToFileThroughHash(urlHash, image))
            return Forbid();

        var fileInfo = await _fileService.GetFileInfoAsync(image.Id);
        // this shouldn't happen
        if (fileInfo is null)
            throw new Exception("File not found.");

        var stream = _fileStorageService.GetFile(image.FilesystemName);
        return File(stream, FileUtils.GetContentType(fileInfo.Path), fileInfo.Name);
    }

    [HttpPost("saveTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SaveTextFile(string directoryPath, string name, int? groupId, string? clientId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var filePath = FileUtils.NormalizePath(Path.Join(directoryPath, name));
        var encryptedFile = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (encryptedFile is null)
        {
            return NotFound("File not found.");
        }

        await _fileStorageService.SaveFileAsync(encryptedFile.FilesystemName, Request.Body);
        var modified = await _fileService.UpdateTimestamp(directoryPath, name, groupId);
        await _publicFileScheduler.ScheduleImageRefreshForEncryptedFile(encryptedFile.Id);

        var syncMessage = new FileSyncChannelMessage(modified, clientId ?? Guid.NewGuid().ToString());
        await _fileSyncService.SendSyncSignalAsync(encryptedFile.Id, syncMessage, HttpContext.RequestAborted);
        return Ok();
    }

    [HttpPost("shared/saveTextFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SaveSharedTextFile(string path, string? clientId)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(path, user);
        if (sharedFile is null)
            return NotFound("File not found.");

        var encryptedFile = await _fileShareService.GetEncryptedFileFromSharedFile(sharedFile);

        await _fileStorageService.SaveFileAsync(encryptedFile.FilesystemName, Request.Body);
        var modified = await _fileService.UpdateTimestamp(encryptedFile);
        await _publicFileScheduler.ScheduleImageRefreshForEncryptedFile(encryptedFile.Id);

        var syncMessage = new FileSyncChannelMessage(modified, clientId ?? Guid.NewGuid().ToString());
        await _fileSyncService.SendSyncSignalAsync(encryptedFile.Id, syncMessage, HttpContext.RequestAborted);
        return Ok();
    }

    [HttpDelete("deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile(string directoryPath, string name, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
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

    [HttpDelete("shared/deleteFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSharedFile(string path)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(path, user);
        if (sharedFile is null)
            return NotFound("File not found.");

        await _fileShareService.Delete(sharedFile);
        return Ok();
    }

    [HttpGet("getFileInfo")]
    [ProducesResponseType<FileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetFileInfo(string filePath, int? groupId)
    {
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
            return Forbid();

        var fileInfo = await _fileService.GetFileInfo(filePath, groupId);
        return Ok(fileInfo);
    }

    [HttpGet("shared/getFileInfo")]
    [ProducesResponseType<FileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSharedFileInfo(string filePath)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(filePath, user);
        if (sharedFile is null)
            return NotFound("File not found.");

        var encryptedFile = await _fileShareService.GetEncryptedFileFromSharedFile(sharedFile);

        var fileInfo = await _fileService.GetFileInfoAsync(encryptedFile.Id);
        if (fileInfo is null)
            return NotFound("File not found.");

        return Ok(fileInfo);
    }

    [AllowAnonymous]
    [HttpGet("public/getFileInfo")]
    [ProducesResponseType<FileResponse>(StatusCodes.Status200OK)]
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
        if (await GetGroupAccess(groupId) == GroupAccessStatus.AccessDenied)
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

    [HttpGet("shared/downloadFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DownloadSharedFile(string path)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(path, user);
        if (sharedFile is null)
            return NotFound("File not found.");

        var encryptedFile = await _fileShareService.GetEncryptedFileFromSharedFile(sharedFile);
        var fileInfo = await _fileService.GetFileInfoAsync(encryptedFile.Id);
        if (fileInfo is null)
            return NotFound("File not found.");

        new FileExtensionContentTypeProvider().TryGetContentType(path, out var contentType);
        var stream = _fileStorageService.GetFile(encryptedFile.FilesystemName);
        return File(stream, contentType ?? "application/octet-stream", fileInfo.Name);
    }

    private async Task HandleFileSync(int encryptedFileId)
    {
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        try
        {
            _logger.LogInformation("File sync connection opened for file with ID {fileId}", encryptedFileId);
            await _webSocketFileSyncService.HandleFileSync(webSocket, encryptedFileId, HttpContext.RequestAborted);
        }
        // Some common exceptions that happen when a connection is closed prematurely
        catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // No need to log the error as we know the reason for this exception
            _logger.LogWarning("WebSocket connection closed prematurely");
        }
        catch (ConnectionAbortedException e)
        {
            _logger.LogWarning(e, "WebSocket connection aborted");
        }
        catch (OperationCanceledException e)
        {
            switch (e.InnerException)
            {
                case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                    // No need to log the error as we know the reason for this exception
                    _logger.LogWarning("WebSocket connection closed prematurely");
                    return;
                case ObjectDisposedException objectDisposedException:
                    _logger.LogWarning(objectDisposedException, "WebSocket connection closed due to a disposed object");
                    return;
                default:
                    throw;
            }
        }
    }

    [Route("/ws/fileSync")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task FileSync(string filePath, int? groupId)
    {
        var encryptedFile = await _fileService.GetEncryptedFileAsync(filePath, groupId);
        if (encryptedFile is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("File not found");
            return;
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("Only WebSocket requests are supported on this endpoint");
            return;
        }

        await HandleFileSync(encryptedFile.Id);
    }

    [Route("/ws/sharedFileSync")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task SharedFileSync(string filePath)
    {
        var user = await _userService.GetCurrentUser();
        var sharedFile = await _fileShareService.GetUserSharedFile(filePath, user);
        if (sharedFile is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("File not found");
            return;
        }

        var encryptedFile = await _fileShareService.GetEncryptedFileFromSharedFile(sharedFile);

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("Only WebSocket requests are supported on this endpoint");
            return;
        }

        await HandleFileSync(encryptedFile.Id);
    }

    [Route("/ws/publicFileSync")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task PublicFileSync(string urlHash)
    {
        var encryptedFile = await _publicFileService.GetEncryptedFileAsync(urlHash);

        if (encryptedFile is null || !await _publicFileService.IsPublicFileActive(urlHash))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("File not found");
            return;
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            HttpContext.Response.ContentType = "text/plain";
            await HttpContext.Response.WriteAsync("Only WebSocket requests are supported on this endpoint");
            return;
        }

        await HandleFileSync(encryptedFile.Id);
    }

    [HttpPost("shareFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    // This API doesn't support groups as group files are already "shared"
    public async Task<ActionResult> ShareFile(string filePath)
    {
        var encryptedFile = await _fileService.GetEncryptedFileAsync(filePath, null);
        if (encryptedFile is null)
            return NotFound("File not found.");

        var hash = await _fileShareService.GenerateShareHash(encryptedFile.Id);
        return Ok(hash);
    }

    [HttpPost("generateSharedFile")]
    public async Task<ActionResult> GenerateSharedFile(string hash, string directoryPath)
    {
        var directory = await _fileServiceUtils.GetDirectory(directoryPath, false, null);
        if (directory is null)
            return NotFound("Directory not found.");
        var directoryOwner = await _directoryService.GetOwner(directory.Id);
        // This should never happen
        if (directoryOwner is null)
            throw new Exception("Directory owner not found.");

        var ownerFromHash = await _fileShareService.GetOwnerFromHash(hash);
        if (ownerFromHash?.Id == directoryOwner.Id)
        {
            return BadRequest("You cannot share a file with yourself.");
        }

        await _fileShareService.GenerateSharedFileFromHash(hash, directory.Id);
        return Ok();
    }
}
