﻿using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Server.Controllers;
using Server.Services;

namespace ServerTests.ControllerTests;

public class TagControllerTests : BaseTests
{
    private readonly TagController _controller;
    private readonly ITagService _tagService;
    private readonly ClaimsPrincipal _claimsPrincipal;

    public TagControllerTests()
    {
        _tagService = Substitute.For<ITagService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        var httpContext = new DefaultHttpContext { User = _claimsPrincipal };
        _controller = new TagController(_tagService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
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