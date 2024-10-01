using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server.Controllers;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class AdminControllerTests : BaseTests
{
    private readonly IAdminService _adminService;
    private readonly AdminController _controller;
    private readonly ClaimsPrincipal _claimsPrincipal;

    public AdminControllerTests()
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test_user")
        ]));
        _adminService = Substitute.For<IAdminService>();
        _controller = new AdminController(_adminService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _claimsPrincipal }
            }
        };
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
            new(new User(0, "user1", "user1", "user1@user.com", "password")),
            new(new User(0, "user2", "user2", "user2@user.com", "password"))
        ];
        
        _adminService.ListUsers().Returns(users);
        
        var result = await _controller.ListUsers();
        
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, okResult.Value);
    }

}