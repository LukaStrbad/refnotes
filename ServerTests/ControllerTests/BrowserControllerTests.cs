using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Server.Controllers;
using Server.Model;
using Server.Services;
using ServerTests.Fixtures;

namespace ServerTests.ControllerTests;

public class BrowserControllerTests : BaseTests, IClassFixture<ControllerFixture<BrowserController>>
{
    private readonly BrowserController _controller;
    private readonly IBrowserService _browserService;

    public BrowserControllerTests(ControllerFixture<BrowserController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _controller = serviceProvider.GetRequiredService<BrowserController>();
        _browserService = serviceProvider.GetRequiredService<IBrowserService>();
    }

    [Fact]
    public async Task List_ReturnsOk_WhenDirectoryExists()
    {
        const string path = "test_path";
        var responseDirectory = new DirectoryDto("test_dir", [], []);

        _browserService.List(null, path).Returns(responseDirectory);

        var result = await _controller.List(path, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(responseDirectory, okResult.Value);
    }

    [Fact]
    public async Task List_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        const string path = "test_path";
        _browserService.List(null, path).Returns((DirectoryDto?)null);

        var result = await _controller.List(path, null);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Directory not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task AddDirectory_ReturnsOk_WhenDirectoryAdded()
    {
        const string path = "/test_path";
        _browserService.AddDirectory(path, null).Returns(Task.CompletedTask);

        var result = await _controller.AddDirectory(path, null);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsOk_WhenDirectoryDeleted()
    {
        const string path = "/test_path";
        _browserService.DeleteDirectory(path, null).Returns(Task.CompletedTask);

        var result = await _controller.DeleteDirectory(path, null);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteDirectory_ReturnsBadRequest_WhenDeletingRootDirectory()
    {
        const string path = "/";
        _browserService.DeleteDirectory(path, null)
            .Returns(Task.FromException(new ArgumentException("Cannot delete root directory.")));

        var result = await _controller.DeleteDirectory(path, null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot delete root directory.", badRequestResult.Value);
    }
}