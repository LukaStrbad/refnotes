﻿using System;
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
    private ClaimsPrincipal _claimsPrincipal;
    private DefaultHttpContext _httpContext;

    public BrowserControllerTests()
    {
        _browserService = Substitute.For<IBrowserService>();
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _httpContext = new DefaultHttpContext { User = _claimsPrincipal };
        _controller = new BrowserController(_browserService)
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