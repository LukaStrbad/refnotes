using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Server.Controllers;
using Data.Db.Model;
using Server.Services;
using ServerTests.Fixtures;

namespace ServerTests.ControllerTests;

public class TagControllerTests : BaseTests, IClassFixture<ControllerFixture<TagController>>
{
    private readonly TagController _controller;
    private readonly ITagService _tagService;
    private readonly IGroupPermissionService _groupPermissionService;

    public TagControllerTests(ControllerFixture<TagController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _tagService = serviceProvider.GetRequiredService<ITagService>();
        _controller = serviceProvider.GetRequiredService<TagController>();
        _groupPermissionService = serviceProvider.GetRequiredService<IGroupPermissionService>();
    }

    [Fact]
    public async Task ListAllTags_ReturnsOk_WhenTagsListed()
    {
        _tagService.ListAllTags().Returns(Task.FromResult(new List<string> { "tag1", "tag2" }));

        var result = await _controller.ListAllTags();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tags = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(["tag1", "tag2"], tags);
    }

    [Fact]
    public async Task ListAllGroupTags_ReturnsOk_WhenTagsListed()
    {
        const int groupId = 1;
        _tagService.ListAllGroupTags(groupId).Returns(Task.FromResult(new List<string> { "tag1", "tag2" }));
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(true);
        
        var result = await _controller.ListAllGroupTags(groupId);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var tags = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(["tag1", "tag2"], tags);
    }

    [Fact]
    public async Task ListAllGroupTags_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        
        var result = await _controller.ListAllGroupTags(groupId);
        
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ListFileTags_ReturnsOk_WhenTagsListed()
    {
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";

        _tagService.ListFileTags(directoryPath, name, null)
            .Returns(Task.FromResult(new List<string> { "tag1", "tag2" }));

        var result = await _controller.ListFileTags(directoryPath, name, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tags = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(["tag1", "tag2"], tags);
    }

    [Fact]
    public async Task ListFileTags_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        
        var result = await _controller.ListFileTags(directoryPath, name, groupId);
        
        Assert.IsType<ForbidResult>(result);
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
    public async Task AddFileTag_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string tag = "test_tag";
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        
        var result = await _controller.AddFileTag(directoryPath, name, tag, groupId);
        
        Assert.IsType<ForbidResult>(result);
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
    
    [Fact]
    public async Task RemoveFileTag_ReturnsForbidden_WhenGroupIsForbidden()
    {
        const int groupId = 1;
        const string directoryPath = "test_dir_path";
        const string name = "test_file_name";
        const string tag = "test_tag";
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId).Returns(false);
        
        var result = await _controller.RemoveFileTag(directoryPath, name, tag, groupId);
        
        Assert.IsType<ForbidResult>(result);
    }
}