using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server.Controllers;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class FileControllerTests : BaseTests
{
    private readonly FileController _controller;
    private readonly IFileService _fileService;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly IFileStorageService _fileStorageService;
    private readonly DefaultHttpContext _httpContext;

    public FileControllerTests()
    {
        _fileService = Substitute.For<IFileService>();
        _fileStorageService = Substitute.For<IFileStorageService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _httpContext = new DefaultHttpContext { User = _claimsPrincipal };
        _controller = new FileController(_fileService, _fileStorageService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
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

        _fileService.AddFile(directoryPath, name).Returns(fileName);

        var formFileCollection = new FormFileCollection { file };
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
            formFileCollection);
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;

        var result = await _controller.AddFile(directoryPath);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, fileStream);
    }

    [Fact]
    public async Task AddTextFile_ReturnsOk_WhenFileAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        const string content = "test content";

        _fileService.AddFile(directoryPath, name).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.AddTextFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
    }

    [Fact]
    public async Task MoveFile_ReturnsOk_WhenFileIsMoved()
    {
        const string oldName = "/dir/file.txt";
        const string newName = "/dir2/file2.txt";

        _fileService.MoveFile(oldName, newName).Returns(Task.CompletedTask);

        var result = await _controller.MoveFile(oldName, newName);

        Assert.IsType<OkResult>(result);
    }


    [Fact]
    public async Task GetFile_ReturnsOk_WhenFileExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var stream = Substitute.For<Stream>();

        _fileService.GetFilesystemFilePath(directoryPath, name).Returns(fileName);
        _fileStorageService.GetFile(fileName).Returns(stream);

        var result = await _controller.GetFile(directoryPath, name);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        _fileService.GetFilesystemFilePath(directoryPath, name).Returns((string?)null);

        var result = await _controller.GetFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetImage_ReturnsOk_WhenImageExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string imageName = "test_file_name.png";
        var stream = Substitute.For<Stream>();

        _fileService.GetFilesystemFilePath(directoryPath, name).Returns(imageName);
        _fileStorageService.GetFile(imageName).Returns(stream);

        var result = await _controller.GetImage(directoryPath, name);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }

    [Fact]
    public async Task GetImage_ReturnsEmptyStream_WhenImageDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name.png";
        _fileService.GetFilesystemFilePath(directoryPath, name).Returns((string?)null);

        var result = await _controller.GetImage(directoryPath, name);

        var okResult = Assert.IsType<FileContentResult>(result);
        Assert.Empty(okResult.FileContents);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_WhenFileDeleted()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.DeleteFile(directoryPath, name).Returns(Task.CompletedTask);

        var result = await _controller.DeleteFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.GetFilesystemFilePath(directoryPath, name).Returns((string?)null);

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

        _fileService.GetFilesystemFilePath(directoryPath, name).Returns(fileName);
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _controller.SaveTextFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
        await _fileStorageService.Received(1).SaveFileAsync(fileName, Arg.Any<Stream>());
        await _fileService.Received(1).UpdateTimestamp(directoryPath, name);
    }

    [Fact]
    public async Task SaveTextFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _fileService.GetFilesystemFilePath(directoryPath, name).Returns((string?)null);

        var result = await _controller.SaveTextFile(directoryPath, name);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("File not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetFileInfo_ReturnsFileInfo()
    {
        const string filePath = "/file.txt";

        _fileService.GetFileInfo(filePath).Returns(Task.FromResult(
            new FileDto("file.txt", ["tag1", "tag2"], 1024, DateTime.UtcNow, DateTime.UtcNow))
        );

        var result = await _controller.GetFileInfo(filePath);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var fileInfo = Assert.IsType<FileDto>(okResult.Value);
        Assert.Equal("file.txt", fileInfo.Name);
    }
}