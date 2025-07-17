using Api.Controllers;
using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ControllerTests;

public sealed class UserControllerTests : IClassFixture<ControllerFixture<UserController>>
{
    private readonly IUserService _userService;
    private readonly IEmailConfirmService _emailConfirmService;
    private readonly UserController _controller;

    public UserControllerTests(ControllerFixture<UserController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _userService = serviceProvider.GetRequiredService<IUserService>();
        _emailConfirmService = serviceProvider.GetRequiredService<IEmailConfirmService>();
        _controller = serviceProvider.GetRequiredService<UserController>();
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsOk_WhenEmailConfirmed()
    {
        var token = Guid.NewGuid().ToString();
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 1534
        };
        _userService.GetCurrentUser().Returns(user);
        _emailConfirmService.ConfirmEmail(token, user.Id).Returns(true);

        var result = await _controller.ConfirmEmail(token);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsBadRequest_WhenEmailNotConfirmed()
    {
        var token = Guid.NewGuid().ToString();
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 1535
        };
        _userService.GetCurrentUser().Returns(user);
        // Simulate email confirmation failure
        _emailConfirmService.ConfirmEmail(token, user.Id).Returns(false);

        var result = await _controller.ConfirmEmail(token);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
