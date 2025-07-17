using Api.Controllers;
using Api.Model;
using Api.Services;
using Api.Services.Schedulers;
using Api.Tests.Fixtures;
using Api.Tests.Mocks;
using Data.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ControllerTests;

public sealed class UserControllerTests : IClassFixture<ControllerFixture<UserController>>
{
    private readonly IUserService _userService;
    private readonly IEmailConfirmService _emailConfirmService;
    private readonly IAuthService _authService;
    private readonly IEmailScheduler _emailScheduler;
    private readonly UserController _controller;
    private readonly RequestCookieCollection _requestCookies;
    private readonly ResponseCookies _responseCookies;

    public UserControllerTests(ControllerFixture<UserController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _userService = serviceProvider.GetRequiredService<IUserService>();
        _emailConfirmService = serviceProvider.GetRequiredService<IEmailConfirmService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _emailScheduler = serviceProvider.GetRequiredService<IEmailScheduler>();
        _controller = serviceProvider.GetRequiredService<UserController>();

        var httpContext = serviceProvider.GetRequiredService<HttpContext>();
        _requestCookies = new RequestCookieCollection();
        _responseCookies = new ResponseCookies();
        httpContext.Request.Cookies.Returns(_requestCookies);
        httpContext.Response.Cookies.Returns(_responseCookies);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void AssertCookiesContainTokens(string accessToken, string refreshToken)
    {
        var hasAccessTokenCookie = _responseCookies.Cookies.TryGetValue("accessToken", out var accessTokenCookie);
        Assert.True(hasAccessTokenCookie);
        Assert.NotNull(accessTokenCookie);
        Assert.Equal(accessToken, accessTokenCookie);

        var hasRefreshTokenCookie = _responseCookies.Cookies.TryGetValue("refreshToken", out var refreshTokenCookie);
        Assert.True(hasRefreshTokenCookie);
        Assert.NotNull(refreshTokenCookie);
        Assert.Equal(refreshToken, refreshTokenCookie);

        var hasRefreshTokenCookieOptions =
            _responseCookies.Options.TryGetValue("refreshToken", out var refreshTokenCookieOptions);
        Assert.True(hasRefreshTokenCookieOptions);
        Assert.NotNull(refreshTokenCookieOptions);
        Assert.True(refreshTokenCookieOptions.HttpOnly);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsOk_WhenEmailConfirmed()
    {
        var token = Guid.NewGuid().ToString();
        var tokens = new Tokens("access-token", new RefreshToken("refresh-token", DateTime.UtcNow.AddDays(7)));
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 1534
        };
        _userService.GetCurrentUser().Returns(user);
        _emailConfirmService.ConfirmEmail(token).Returns((user, true));
        _authService.ForceLogin(user.Id).Returns(tokens);

        var result = await _controller.ConfirmEmail(token);

        Assert.IsType<OkResult>(result);
        AssertCookiesContainTokens(tokens.AccessToken, tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsBadRequest_WhenEmailNotConfirmed()
    {
        var token = Guid.NewGuid().ToString();
        // Simulate email confirmation failure
        _emailConfirmService.ConfirmEmail(token).Returns((null, false));

        var result = await _controller.ConfirmEmail(token);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResendEmailConfirmation_ReturnsOk_WhenEmailConfirmationIsSent()
    {
        var token = Guid.NewGuid().ToString();
        const string lang = "en";
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 432
        };
        _userService.GetCurrentUser().Returns(user);
        _emailConfirmService.GenerateToken(user.Id).Returns(token);

        var result = await _controller.ResendEmailConfirmation(lang);

        Assert.IsType<OkObjectResult>(result);
        await _emailScheduler.Received(1).ScheduleVerificationEmail(user.Email, user.Name, token, lang);
    }

    [Fact]
    public async Task ResendEmailConfirmation_ReturnsBadRequest_WhenEmailAlreadyConfirmed()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 432,
            EmailConfirmed = true
        };
        _userService.GetCurrentUser().Returns(user);

        var result = await _controller.ResendEmailConfirmation("en");

        Assert.IsType<BadRequestObjectResult>(result);
        await _emailScheduler.DidNotReceiveWithAnyArgs()
            .ScheduleVerificationEmail(user.Email, user.Name, Arg.Any<string>(), "en");
    }
}
