using Api.Controllers;
using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Api.Tests.Mocks;
using Data.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.ControllerTests;

public class AuthControllerTests : BaseTests, IClassFixture<ControllerFixture<AuthController>>
{
    private readonly IAuthService _authService;
    private readonly AuthController _controller;
    private readonly RequestCookieCollection _requestCookies;
    private readonly ResponseCookies _responseCookies;

    public AuthControllerTests(ControllerFixture<AuthController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        var httpContext = serviceProvider.GetRequiredService<HttpContext>();
        _requestCookies = new RequestCookieCollection();
        _responseCookies = new ResponseCookies();
        httpContext.Request.Cookies.Returns(_requestCookies);
        httpContext.Response.Cookies.Returns(_responseCookies);

        serviceProvider.GetRequiredService<IConfiguration>()["CookieDomain"] = "localhost";

        _controller = serviceProvider.GetRequiredService<AuthController>();
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
    public async Task Login_ReturnsOk_WhenUserExists()
    {
        var credentials = new UserCredentials("test", "test");
        var tokens = new Tokens("access", new RefreshToken("token", DateTime.Now));
        _authService.Login(credentials).Returns(tokens);

        var result = await _controller.Login(credentials);
        Assert.IsType<OkResult>(result.Result);

        AssertCookiesContainTokens(tokens.AccessToken, tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Login_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var credentials = new UserCredentials("test", "test");
        _authService.Login(credentials).ThrowsAsync(new UserNotFoundException("User not found"));

        var result = await _controller.Login(credentials);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var credentials = new UserCredentials("test", "test");
        _authService.Login(credentials).ThrowsAsync(new UnauthorizedException());

        var result = await _controller.Login(credentials);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenUserIsRegistered()
    {
        var newUser = new User(0, "newUser", "newUser", "newUser@newUser.com", "password");
        var tokens = new Tokens("access", new RefreshToken("token", DateTime.Now));
        _authService.Register(newUser).Returns(tokens);

        var result = await _controller.Register(newUser);
        Assert.IsType<OkResult>(result.Result);

        AssertCookiesContainTokens(tokens.AccessToken, tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserExists()
    {
        var newUser = new User(0, "test", "test", "test@test.com", "password");
        _authService.Register(newUser).ThrowsAsync(new UserExistsException("User already exists"));

        var result = await _controller.Register(newUser);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("User already exists", badRequestResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenTokensAreRefreshed()
    {
        const string initialAccessToken = "access1";
        const string accessToken = "access2";
        const string initialRefreshToken = "refresh1";
        const string refreshToken = "refresh2";

        _requestCookies["accessToken"] = initialAccessToken;
        _requestCookies["refreshToken"] = initialRefreshToken;
        var tokens = new Tokens(accessToken, new RefreshToken(refreshToken, DateTime.Now));
        _authService.RefreshAccessToken(initialAccessToken, initialRefreshToken).Returns(tokens);

        var result = await _controller.RefreshToken();
        Assert.IsType<OkResult>(result.Result);

        AssertCookiesContainTokens(accessToken, refreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenNoRefreshTokenIsProvided()
    {
        _requestCookies["accessToken"] = "access";
        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No refresh token provided", badRequestResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _requestCookies["accessToken"] = "access";
        _requestCookies["refreshToken"] = "refresh";
        _authService.RefreshAccessToken("access", "refresh").ThrowsAsync(new UserNotFoundException("User not found"));

        var result = await _controller.RefreshToken();
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenRefreshTokenIsInvalid()
    {
        _requestCookies["accessToken"] = "access";
        _requestCookies["refreshToken"] = "refresh";
        _authService.RefreshAccessToken("access", "refresh")
            .ThrowsAsync(new RefreshTokenInvalid("Invalid refresh token"));

        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid refresh token", badRequestResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenAccessTokenIsMalformed()
    {
        _requestCookies["refreshToken"] = "refresh";
        _requestCookies["accessToken"] = "access";
        _authService.RefreshAccessToken("access", "refresh")
            .ThrowsAsync(new SecurityTokenMalformedException("Malformed access token"));

        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Malformed access token", badRequestResult.Value);
    }
}
