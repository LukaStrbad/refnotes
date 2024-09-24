using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Server;
using Server.Controllers;
using Server.Db;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class BrowserControllerTests
{
    private readonly BrowserController _controller;
    private readonly IBrowserServiceRepository _browserServiceRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly AppConfiguration _appConfig;
    private readonly RefNotesContext _db;
    private ClaimsPrincipal _claimsPrincipal;
    private readonly IFileService _fileService;

    public BrowserControllerTests()
    {
        _appConfig = new AppConfiguration { DataDir = "test_data_dir" };
        _db = Substitute.For<RefNotesContext>(_appConfig);
        _encryptionService = Substitute.For<IEncryptionService>();
        _browserServiceRepository = Substitute.For<IBrowserServiceRepository>();
        _fileService = Substitute.For<IFileService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _controller = new BrowserController(_browserServiceRepository, _fileService)
        {
            ControllerContext = new ControllerContext
                { HttpContext = new DefaultHttpContext { User = _claimsPrincipal } }
        };
    }

    [Fact]
    public async Task List_ReturnsOk_WhenDirectoryExists()
    {
        const string path = "test_path";
        var responseDirectory = new ResponseDirectory
        {
            Name = "test_dir",
            Files = [],
            Directories = []
        };
        
        _browserServiceRepository.List(_claimsPrincipal, path).Returns(responseDirectory);

        var result = await _controller.List(path);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(responseDirectory, okResult.Value);
    }
    
    [Fact]
    public async Task List_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string path = "test_path";
        _browserServiceRepository.List(_claimsPrincipal, path).Returns((ResponseDirectory?)null);

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
        
        _browserServiceRepository.AddFile(_claimsPrincipal, directoryPath, name).Returns(fileName);

        var result = await _controller.AddFile(directoryPath, name, file);

        Assert.IsType<OkResult>(result);
    }
    
    [Fact]
    public async Task GetFile_ReturnsOk_WhenFileExists()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string fileName = "test_file_name";
        var stream = Substitute.For<Stream>();
        
        _browserServiceRepository.GetFilesystemFilePath(_claimsPrincipal, directoryPath, name).Returns(fileName);
        _fileService.GetFile(fileName).Returns(stream);

        var result = await _controller.GetFile(directoryPath, name);

        var okResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(stream, okResult.FileStream);
    }
    
    
}