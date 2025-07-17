using Api.Controllers;
using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.ControllerTests;

public class AdminControllerTests : BaseTests, IClassFixture<ControllerFixture<AdminController>>
{
    private readonly AdminController _controller;
    private readonly IAdminService _adminService;

    public AdminControllerTests(ControllerFixture<AdminController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _controller = serviceProvider.GetRequiredService<AdminController>();
        _adminService = serviceProvider.GetRequiredService<IAdminService>();
    }

    [Fact]
    public async Task ModifyRoles_ReturnsOk_WhenRolesModified()
    {
        var roles = new List<string> { "admin" };
        var modifyRolesRequest = new ModifyRolesRequest("test_user", roles, []);

        _adminService.ModifyRoles(modifyRolesRequest).Returns(roles);

        var result = await _controller.ModifyRoles(modifyRolesRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(roles, okResult.Value);
    }

    [Fact]
    public async Task ModifyRoles_ReturnsNotFound_WhenUserNotFound()
    {
        var roles = new List<string> { "admin" };
        var modifyRolesRequest = new ModifyRolesRequest("test_user", roles, []);

        _adminService.ModifyRoles(modifyRolesRequest).ThrowsAsync(new UserNotFoundException("User not found"));

        var result = await _controller.ModifyRoles(modifyRolesRequest);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

    [Fact]
    public async Task ListUsers_ReturnsUsersList()
    {
        List<ResponseUser> users =
        [
            new(new User("user1", "user1", "user1@user.com", "password")),
            new(new User("user2", "user2", "user2@user.com", "password"))
        ];

        _adminService.ListUsers().Returns(users);

        var result = await _controller.ListUsers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, okResult.Value);
    }
}
