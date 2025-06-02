using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Server.Controllers;
using Server.Services;
using ServerTests.Fixtures;

namespace ServerTests.ControllerTests;

public class TagControllerTests : BaseTests, IClassFixture<ControllerFixture<TagController>>
{
    private readonly TagController _controller;
    private readonly ITagService _tagService;

    public TagControllerTests(ControllerFixture<TagController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _tagService = serviceProvider.GetRequiredService<ITagService>();
        _controller = serviceProvider.GetRequiredService<TagController>();
    }

    [Fact]
    public async Task ListAllTags_ReturnsOk_WhenTagsListed()
    {
        _tagService.ListAllTags().Returns(Task.FromResult(new List<string> { "tag1", "tag2" }));

        var result = await _controller.ListAllTags();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tags = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(["tag1", "tag2"], tags);
    }

    [Fact]
    public async Task ListFileTags_ReturnsOk_WhenTagsListed()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _tagService.ListFileTags(directoryPath, name, null)
            .Returns(Task.FromResult(new List<string> { "tag1", "tag2" }));

        var result = await _controller.ListFileTags(directoryPath, name, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tags = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(["tag1", "tag2"], tags);
    }

    [Fact]
    public async Task AddFileTag_ReturnsOk_WhenTagAdded()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string tag = "test_tag";

        _tagService.AddFileTag(directoryPath, name, tag, null).Returns(Task.CompletedTask);

        var result = await _controller.AddFileTag(directoryPath, name, tag, null);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveFileTag_ReturnsOk_WhenTagRemoved()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string tag = "test_tag";

        _tagService.RemoveFileTag(directoryPath, name, tag, null).Returns(Task.CompletedTask);

        var result = await _controller.RemoveFileTag(directoryPath, name, tag, null);

        Assert.IsType<OkResult>(result);
    }
}