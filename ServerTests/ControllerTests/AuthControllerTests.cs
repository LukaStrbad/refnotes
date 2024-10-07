using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server.Controllers;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ControllerTests;

public class AuthControllerTests : BaseTests
{
    private readonly IAuthService _authService;
    private readonly AuthController _controller;
    private readonly IRequestCookieCollection _requestCookies;
    private readonly IResponseCookies _responseCookies;
    private readonly HttpContext _httpContext;

    public AuthControllerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _requestCookies = Substitute.For<IRequestCookieCollection>();
        _responseCookies = Substitute.For<IResponseCookies>();
        _httpContext = Substitute.For<HttpContext>();
        _httpContext.Request.Cookies.Returns(_requestCookies);
        _httpContext.Response.Cookies.Returns(_responseCookies);
        _controller = new AuthController(_authService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    private void AssertCookieContainsRefreshToken(string refreshToken)
    {
        var cookiesCall = _responseCookies.ReceivedCalls()
            .FirstOrDefault(x => x.GetMethodInfo().Name == nameof(IResponseCookies.Append));
        Assert.NotNull(cookiesCall);
        var cookiesArgs = cookiesCall.GetArguments();
        Assert.Equal(3, cookiesArgs.Length);
        Assert.Equal("refreshToken", cookiesArgs[0]);
        Assert.Equal(refreshToken, cookiesArgs[1]);
        var thirdArg = Assert.IsType<CookieOptions>(cookiesArgs[2]);
        Assert.True(thirdArg.HttpOnly);
    }

    private void SetBody(string value)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
        _httpContext.Request.Body.Returns(stream);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenUserExists()
    {
        var credentials = new UserCredentials("test", "test");
        var tokens = new Tokens("access", new RefreshToken("token", DateTime.Now));
        _authService.Login(credentials).Returns(tokens);

        var result = await _controller.Login(credentials);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        AssertCookieContainsRefreshToken(tokens.RefreshToken.Token);
        Assert.Equal(tokens.AccessToken, okResult.Value);
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
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        AssertCookieContainsRefreshToken(tokens.RefreshToken.Token);
        Assert.Equal(tokens.AccessToken, okResult.Value);
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

        _requestCookies["refreshToken"].Returns(initialRefreshToken);
        var tokens = new Tokens(accessToken, new RefreshToken(refreshToken, DateTime.Now));
        _authService.RefreshAccessToken(initialAccessToken, initialRefreshToken).Returns(tokens);
        
        SetBody(initialAccessToken);
        var result = await _controller.RefreshToken();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(accessToken, okResult.Value);
        AssertCookieContainsRefreshToken(refreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenNoRefreshTokenIsProvided()
    {
        _requestCookies["refreshToken"].Returns((string?)null);

        SetBody("access");
        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No refresh token provided", badRequestResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _requestCookies["refreshToken"].Returns("refresh");
        _authService.RefreshAccessToken("access", "refresh").ThrowsAsync(new UserNotFoundException("User not found"));

        SetBody("access");
        var result = await _controller.RefreshToken();
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenRefreshTokenIsInvalid()
    {
        _requestCookies["refreshToken"].Returns("refresh");
        _authService.RefreshAccessToken("access", "refresh")
            .ThrowsAsync(new RefreshTokenInvalid("Invalid refresh token"));

        SetBody("access");
        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid refresh token", badRequestResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenAccessTokenIsMalformed()
    {
        _requestCookies["refreshToken"].Returns("refresh");
        _authService.RefreshAccessToken("access", "refresh")
            .ThrowsAsync(new SecurityTokenMalformedException("Malformed access token"));

        SetBody("access");
        var result = await _controller.RefreshToken();
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Malformed access token", badRequestResult.Value);
    }
}