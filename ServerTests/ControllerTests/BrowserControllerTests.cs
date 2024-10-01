using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server;
using Server.Controllers;
using Server.Db;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class BrowserControllerTests : BaseTests
{
    private readonly BrowserController _controller;
    private readonly IBrowserService _browserService;
    private readonly IEncryptionService _encryptionService;
    private readonly AppConfiguration _appConfig;
    private readonly RefNotesContext _db;
    private ClaimsPrincipal _claimsPrincipal;
    private readonly IFileService _fileService;

    public BrowserControllerTests()
    {
        _appConfig = new AppConfiguration { DataDir = "test_data_dir" };
        var options = new DbContextOptionsBuilder<RefNotesContext>().UseInMemoryDatabase("test_db").Options;
        _db = Substitute.For<RefNotesContext>(options);
        _encryptionService = Substitute.For<IEncryptionService>();
        _browserService = Substitute.For<IBrowserService>();
        _fileService = Substitute.For<IFileService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _controller = new BrowserController(_browserService, _fileService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _claimsPrincipal }
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

        var result = await _controller.AddFile(directoryPath, name);

        Assert.IsType<OkResult>(result);
        await _fileService.Received(1).SaveFile(fileName, fileStream);
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
}