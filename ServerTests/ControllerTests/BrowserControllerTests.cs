using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server;
using Server.Controllers;
using Server.Db;
using Server.Exceptions;
using Server.Model;
using Server.Services;
using Xunit;

namespace ServerTests.ControllerTests;

public class BrowserControllerTests : BaseTests
{
    private readonly BrowserController _controller;
    private readonly IBrowserService _browserService;
    private readonly IEncryptionService _encryptionService;
    private readonly AppConfiguration _appConfig;
    private ClaimsPrincipal _claimsPrincipal;
    private readonly IFileService _fileService;
    private DefaultHttpContext _httpContext;

    public BrowserControllerTests()
    {
        _appConfig = new AppConfiguration { DataDir = "test_data_dir" };
        _encryptionService = Substitute.For<IEncryptionService>();
        _browserService = Substitute.For<IBrowserService>();
        _fileService = Substitute.For<IFileService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _httpContext = new DefaultHttpContext { User = _claimsPrincipal };
        _controller = new BrowserController(_browserService, _fileService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    [Fact]
    public async Task List_ReturnsOk_WhenDirectoryExists()
    {
        const string path = "test_path";
        var responseDirectory = new ResponseDirectory("test_dir", [], []);

        _browserService.List(_claimsPrincipal, path).Returns(responseDirectory);

        var result = await _controller.List(path);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(responseDirectory, okResult.Value);
    }

    [Fact]
    public async Task List_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string path = "test_path";
        _browserService.List(_claimsPrincipal, path).Returns((ResponseDirectory?)null);

        var result = await _controller.List(path);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Directory not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task AddDirectory_ReturnsOk_WhenDirectoryAdded()
    {
        const string path = "/test_path";
        _browserService.AddDirectory(_claimsPrincipal, path).Returns(Task.CompletedTask);

        var result = await _controller.AddDirectory(path);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task AddDirectory_ReturnsBadRequest_WhenDirectoryAlreadyExists()
    {
        const string path = "/test_path";
        _browserService.AddDirectory(_claimsPrincipal, path).Returns(Task.FromException(new Exception("Directory already exists.")));

        var result = await _controller.AddDirectory(path);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Directory already exists.", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsOk_WhenDirectoryDeleted()
    {
        const string path = "/test_path";
        _browserService.DeleteDirectory(_claimsPrincipal, path).Returns(Task.CompletedTask);

        var result = await _controller.DeleteDirectory(path);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsBadRequest_WhenDeletingRootDirectory()
    {
        const string path = "/";
        _browserService.DeleteDirectory(_claimsPrincipal, path).Returns(Task.FromException(new ArgumentException("Cannot delete root directory.")));

        var result = await _controller.DeleteDirectory(path);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot delete root directory.", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsBadRequest_WhenDirectoryNotEmpty()
    {
        const string path = "/test_path";
        _browserService.DeleteDirectory(_claimsPrincipal, path).Returns(Task.FromException(new DirectoryNotEmptyException("Directory not empty.")));

        var result = await _controller.DeleteDirectory(path);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Directory not empty.", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string path = "/test_path";
        _browserService.DeleteDirectory(_claimsPrincipal, path).Returns(Task.FromException(new DirectoryNotFoundException("Directory not found.")));

        var result = await _controller.DeleteDirectory(path);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Directory not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task AddFile_ReturnsOk_WhenFileAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var file = Substitute.For<IFormFile>();
        var fileStream = new MemoryStream("file content"u8.ToArray());
        file.OpenReadStream().Returns(fileStream);
        file.FileName.Returns(name);

        _browserService.AddFile(_claimsPrincipal, directoryPath, name).Returns(fileName);

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath);

        Assert.IsType<OkResult>(result);
        await _fileService.Received(1).SaveFileAsync(fileName, fileStream);
    }

    [Fact]
    public async Task AddFile_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        var file = Substitute.For<IFormFile>();
        var fileStream = new MemoryStream("file content"u8.ToArray());
        file.OpenReadStream().Returns(fileStream);
        file.FileName.Returns(name);

        _browserService.AddFile(_claimsPrincipal, directoryPath, name)
            .Returns(Task.FromException<string>(new DirectoryNotFoundException("Directory not found.")));

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Directory not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task Task_ReturnsBadRequest_WhenFileAlreadyExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        var file = Substitute.For<IFormFile>();
        var fileStream = new MemoryStream("file content"u8.ToArray());
        file.OpenReadStream().Returns(fileStream);
        file.FileName.Returns(name);

        _browserService.AddFile(_claimsPrincipal, directoryPath, name)
            .Returns(Task.FromException<string>(new FileAlreadyExistsException("File already exists.")));

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File already exists.", badRequestResult.Value);
    }

    [Fact]
    public async Task AddTextFile_ReturnsOk_WhenFileAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        _browserService.AddFile(_claimsPrincipal, directoryPath, name).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.AddTextFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
        await _fileService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task GetFile_ReturnsOk_WhenFileExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var stream = Substitute.For<Stream>();

        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns(fileName);
        _fileService.GetFile(fileName).Returns(stream);

        var result = await _controller.GetFile(directoryPath, name);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns((string?)null);

        var result = await _controller.GetFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_WhenFileDeleted()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _browserService.DeleteFile(_claimsPrincipal, directoryPath, name).Returns(Task.CompletedTask);

        var result = await _controller.DeleteFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns((string?)null);

        var result = await _controller.DeleteFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }
    
    [Fact]
    public async Task SaveTextFile_ReturnsOk_WhenFileSaved()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.SaveTextFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
        await _fileService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
    }
    
    [Fact]
    public async Task SaveTextFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns((string?)null);

        var result = await _controller.SaveTextFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }
    
    [Fact]
    public async Task SaveTextFile_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _browserService.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name)
            .Returns(Task.FromException<string?>(new DirectoryNotFoundException("Directory not found.")));

        var result = await _controller.SaveTextFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Directory not found.", notFoundResult.Value);
    }
}
