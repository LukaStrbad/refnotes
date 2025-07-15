using Api.Controllers;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Api.Utils;
using Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Api.Tests.ControllerTests;

public sealed class FavoriteControllerTests : BaseTests, IClassFixture<ControllerFixture<FavoriteController>>
{
    private readonly FavoriteController _controller;
    private readonly IFileService _fileService;
    private readonly IFavoriteService _favoriteService;
    private readonly IFileServiceUtils _fileServiceUtils;
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly User _testUser = new(123, "test", "test", "test@test.com", "password");

    public FavoriteControllerTests(ControllerFixture<FavoriteController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _controller = serviceProvider.GetRequiredService<FavoriteController>();
        _fileService = serviceProvider.GetRequiredService<IFileService>();
        _favoriteService = serviceProvider.GetRequiredService<IFavoriteService>();
        _fileServiceUtils = serviceProvider.GetRequiredService<IFileServiceUtils>();
        var userService = serviceProvider.GetRequiredService<IUserService>();
        _groupPermissionService = serviceProvider.GetRequiredService<IGroupPermissionService>();

        userService.GetCurrentUser().Returns(_testUser);
    }

    [Fact]
    public async Task FavoriteFile_ReturnsOk_WhenFileFavorited()
    {
        const string filePath = "/file.md";
        var file = new EncryptedFile("test.bin", "file.md");
        _fileService.GetEncryptedFileAsync(filePath, null).Returns(file);

        var result = await _controller.FavoriteFile(filePath, null);

        Assert.IsType<OkResult>(result);
        await _favoriteService.Received(1).FavoriteFile(file);
    }

    [Fact]
    public async Task FavoriteFile_ReturnsNotFound_WhenFileNotFound()
    {
        const string filePath = "/file.md";
        _fileService.GetEncryptedFileAsync(filePath, null).ReturnsNull();

        var result = await _controller.FavoriteFile(filePath, null);

        Assert.IsType<NotFoundObjectResult>(result);
        await _favoriteService.DidNotReceiveWithAnyArgs().FavoriteFile(Arg.Any<EncryptedFile>());
    }

    [Fact]
    public async Task FavoriteFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        _groupPermissionService.HasGroupAccessAsync(_testUser, 1).Returns(false);

        var result = await _controller.FavoriteFile("/file.md", 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UnfavoriteFile_ReturnsOk_WhenFileUnfavorited()
    {
        const string filePath = "/file.md";
        var file = new EncryptedFile("test.bin", "file.md");
        _fileService.GetEncryptedFileAsync(filePath, null).Returns(file);

        var result = await _controller.UnfavoriteFile(filePath, null);

        Assert.IsType<OkResult>(result);
        await _favoriteService.Received(1).UnfavoriteFile(file);
    }

    [Fact]
    public async Task UnfavoriteFile_ReturnsNotFound_WhenFileNotFound()
    {
        const string filePath = "/file.md";
        _fileService.GetEncryptedFileAsync(filePath, null).ReturnsNull();

        var result = await _controller.UnfavoriteFile(filePath, null);

        Assert.IsType<NotFoundObjectResult>(result);
        await _favoriteService.DidNotReceiveWithAnyArgs().UnfavoriteFile(Arg.Any<EncryptedFile>());
    }

    [Fact]
    public async Task UnfavoriteFile_ReturnsForbidden_WhenGroupIsForbidden()
    {
        _groupPermissionService.HasGroupAccessAsync(_testUser, 1).Returns(false);

        var result = await _controller.UnfavoriteFile("/file.md", 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetFavoriteFiles_ReturnsFiles()
    {
        var favorites = new List<FileFavoriteDetails>
        {
            new(new FileDto("test.txt", "/test.txt", [], 1024, DateTime.UtcNow, DateTime.UtcNow),
                null, DateTime.UtcNow),
            new(new FileDto("test2.txt", "/test2.txt", [], 1024, DateTime.UtcNow, DateTime.UtcNow),
                null, DateTime.UtcNow)
        };
        _favoriteService.GetFavoriteFiles().Returns(favorites);

        var result = await _controller.GetFavoriteFiles();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedFiles = Assert.IsType<IEnumerable<FileFavoriteDetails>>(okResult.Value, exactMatch: false);

        Assert.Equal(favorites, returnedFiles);
    }

    [Fact]
    public async Task FavoriteDirectory_ReturnsOk_WhenDirectoryFavorited()
    {
        const string directoryPath = "/directory";
        var dir = new EncryptedDirectory(directoryPath, _testUser);

        _fileServiceUtils.GetDirectory(directoryPath, false, null).Returns(dir);

        var result = await _controller.FavoriteDirectory(directoryPath, null);

        Assert.IsType<OkResult>(result);
        await _favoriteService.Received(1).FavoriteDirectory(dir);
    }

    [Fact]
    public async Task FavoriteDirectory_ReturnsNotFound_WhenDirectoryNotFound()
    {
        const string directoryPath = "/directory";
        _fileServiceUtils.GetDirectory(directoryPath, false, null).ReturnsNull();

        var result = await _controller.FavoriteDirectory(directoryPath, null);

        Assert.IsType<NotFoundObjectResult>(result);
        await _favoriteService.DidNotReceiveWithAnyArgs().FavoriteDirectory(Arg.Any<EncryptedDirectory>());
    }

    [Fact]
    public async Task FavoriteDirectory_ReturnsForbidden_WhenGroupIsForbidden()
    {
        _groupPermissionService.HasGroupAccessAsync(_testUser, 1).Returns(false);

        var result = await _controller.FavoriteDirectory("/directory", 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UnfavoriteDirectory_ReturnsOk_WhenDirectoryUnfavorited()
    {
        const string directoryPath = "/directory";
        var dir = new EncryptedDirectory(directoryPath, _testUser);

        _fileServiceUtils.GetDirectory(directoryPath, false, null).Returns(dir);

        var result = await _controller.UnfavoriteDirectory(directoryPath, null);

        Assert.IsType<OkResult>(result);
        await _favoriteService.Received(1).UnfavoriteDirectory(dir);
    }

    [Fact]
    public async Task UnfavoriteDirectory_ReturnsNotFound_WhenDirectoryNotFound()
    {
        const string directoryPath = "/directory";
        _fileServiceUtils.GetDirectory(directoryPath, false, null).ReturnsNull();

        var result = await _controller.UnfavoriteDirectory(directoryPath, null);

        Assert.IsType<NotFoundObjectResult>(result);
        await _favoriteService.DidNotReceiveWithAnyArgs().UnfavoriteDirectory(Arg.Any<EncryptedDirectory>());
    }

    [Fact]
    public async Task UnfavoriteDirectory_ReturnsForbidden_WhenGroupIsForbidden()
    {
        _groupPermissionService.HasGroupAccessAsync(_testUser, 1).Returns(false);

        var result = await _controller.UnfavoriteDirectory("/directory", 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetFavoriteDirectories_ReturnsDirectories()
    {
        var favorites = new List<DirectoryFavoriteDetails>
        {
            new("/test1", null, DateTime.UtcNow),
            new("/test2", null, DateTime.UtcNow)
        };
        _favoriteService.GetFavoriteDirectories().Returns(favorites);

        var result = await _controller.GetFavoriteDirectories();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDirectories =
            Assert.IsType<IEnumerable<DirectoryFavoriteDetails>>(okResult.Value, exactMatch: false);

        Assert.Equal(favorites, returnedDirectories);
    }
}
