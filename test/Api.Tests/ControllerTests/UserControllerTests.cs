using Api.Controllers;
using Api.Exceptions;
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
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.ControllerTests;

public sealed class UserControllerTests : IClassFixture<ControllerFixture<UserController>>
{
    private readonly IUserService _userService;
    private readonly IEmailConfirmService _emailConfirmService;
    private readonly IAuthService _authService;
    private readonly IEmailScheduler _emailScheduler;
    private readonly IPasswordResetService _passwordResetService;
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
        _passwordResetService = serviceProvider.GetRequiredService<IPasswordResetService>();
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
    public async Task AccountInfo_ReturnsAccountInfo()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 1534,
            Roles = ["administrator", "user"]
        };
        _userService.GetCurrentUser().Returns(user);

        var result = await _controller.AccountInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(user.Id, userResponse.Id);
        Assert.Equal(user.Username, userResponse.Username);
        Assert.Equal(user.Name, userResponse.Name);
        Assert.Equal(user.Email, userResponse.Email);
        Assert.Equal(user.Roles, userResponse.Roles);
        Assert.Equal(user.EmailConfirmed, userResponse.EmailConfirmed);
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

    [Fact]
    public async Task UpdatePasswordByToken_ReturnsOk_WhenPasswordUpdated()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 4379,
            EmailConfirmed = true
        };
        var request = new UpdatePasswordByTokenRequest(user.Username, "password123", Guid.NewGuid().ToString());
        _userService.GetByUsername(user.Username).Returns(user);
        _passwordResetService.ValidateToken(user.Id, request.Token).Returns(true);

        var result = await _controller.UpdatePasswordByToken(request);

        Assert.IsType<OkResult>(result);
        await _userService.Received(1).UpdatePassword(Arg.Is<UserCredentials>(cred =>
            cred.Username == user.Username && cred.Password == request.Password));
        await _passwordResetService.Received(1).DeleteTokensForUser(user.Id);
    }

    [Fact]
    public async Task UpdatePasswordByToken_ReturnsBadRequest_WhenEmailIsNotConfirmed()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 4380,
            EmailConfirmed = false
        };
        var request = new UpdatePasswordByTokenRequest(user.Username, "password123", Guid.NewGuid().ToString());
        _userService.GetByUsername(user.Username).Returns(user);

        var result = await _controller.UpdatePasswordByToken(request);

        var objResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorCodes.EmailNotConfirmed, objResult.Value);
        await _userService.DidNotReceiveWithAnyArgs().UpdatePassword(Arg.Any<UserCredentials>());
    }
    
    [Fact]
    public async Task UpdatePasswordByToken_ReturnsBadRequest_WhenTokenIsInvalid()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 4381,
            EmailConfirmed = true
        };
        var request = new UpdatePasswordByTokenRequest(user.Username, "password123", Guid.NewGuid().ToString());
        _userService.GetByUsername(user.Username).Returns(user);
        _passwordResetService.ValidateToken(user.Id, request.Token).Returns(false);

        var result = await _controller.UpdatePasswordByToken(request);

        var objResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ErrorCodes.InvalidOrExpiredToken, objResult.Value);

        await _userService.DidNotReceiveWithAnyArgs().UpdatePassword(Arg.Any<UserCredentials>());
    }

    [Fact]
    public async Task SendPasswordResetEmail_ReturnsOk_WhenEmailIsSent()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 786,
            EmailConfirmed = true
        };
        var token = Guid.NewGuid().ToString();
        _userService.GetByUsername(user.Username).Returns(user);
        _passwordResetService.GenerateToken(user.Id).Returns(token);

        var result = await _controller.SendPasswordResetEmail(user.Username, "en");

        Assert.IsType<OkResult>(result);
        await _emailScheduler.Received(1)
            .SchedulePasswordResetEmail(user, token, "en");
    }

    [Fact]
    public async Task SendPasswordResetEmail_ReturnsBadRequest_WhenEmailIsNotConfirmed()
    {
        var user = new User("test_user", "Test User", "test@test.com", "password123")
        {
            Id = 786,
            EmailConfirmed = false
        };
        _userService.GetByUsername(user.Username).Returns(user);

        var result = await _controller.SendPasswordResetEmail(user.Username, "en");

        Assert.IsType<BadRequestObjectResult>(result);
        await _emailScheduler.DidNotReceiveWithAnyArgs()
            .SchedulePasswordResetEmail(user, Arg.Any<string>(), "en");
    }

    [Fact]
    public async Task EditUser_ReturnsOk_WhenUserIsEdited()
    {
        // Arrange
        var currentUser = new User("test_user", "Test User", "test@example.com", "password123")
        {
            Id = 123
        };
        var editUserRequest = new EditUserRequest("New Name", "new_username", "test@example.com");
        var updatedUser = new User(editUserRequest.NewUsername, editUserRequest.NewName, editUserRequest.NewEmail,
            "password123")
        {
            Id = 123
        };
        var tokens = new Tokens("access-token", new RefreshToken("refresh-token", DateTime.UtcNow.AddDays(7)));
        _userService.GetCurrentUser().Returns(currentUser);
        _userService.EditUser(currentUser.Id, editUserRequest).Returns(updatedUser);
        _authService.ForceLogin(updatedUser.Id).Returns(tokens);

        // Act
        var result = await _controller.EditUser(editUserRequest, "en");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(updatedUser.Id, userResponse.Id);
        Assert.Equal(updatedUser.Username, userResponse.Username);
        Assert.Equal(updatedUser.Name, userResponse.Name);
        Assert.Equal(updatedUser.Email, userResponse.Email);

        await _emailScheduler.DidNotReceiveWithAnyArgs()
            .ScheduleVerificationEmail(updatedUser.Email, updatedUser.Name, Arg.Any<string>(), Arg.Any<string>());
        AssertCookiesContainTokens(tokens.AccessToken, tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task EditUser_SchedulesEmail_WhenEmailIsDifferent()
    {
        // Arrange
        var currentUser = new User("test_user", "Test User", "test@example.com", "password123")
        {
            Id = 123
        };
        var editUserRequest = new EditUserRequest("New Name", "new_username", "new_user@example.com");
        var updatedUser = new User(editUserRequest.NewUsername, editUserRequest.NewName, editUserRequest.NewEmail,
            "password123")
        {
            Id = 123
        };
        var token = Guid.NewGuid().ToString();
        var tokens = new Tokens("access-token", new RefreshToken("refresh-token", DateTime.UtcNow.AddDays(7)));
        _userService.GetCurrentUser().Returns(currentUser);
        _userService.EditUser(currentUser.Id, editUserRequest).Returns(updatedUser);
        _emailConfirmService.GenerateToken(currentUser.Id).Returns(token);
        _authService.ForceLogin(updatedUser.Id).Returns(tokens);

        // Act
        var result = await _controller.EditUser(editUserRequest, "en");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userResponse = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(updatedUser.Id, userResponse.Id);
        Assert.Equal(updatedUser.Username, userResponse.Username);
        Assert.Equal(updatedUser.Name, userResponse.Name);
        Assert.Equal(updatedUser.Email, userResponse.Email);

        await _userService.Received(1).UnconfirmEmail(updatedUser.Id);
        await _emailConfirmService.Received(1).DeleteTokensForUser(updatedUser.Id);
        await _passwordResetService.Received(1).DeleteTokensForUser(updatedUser.Id);
        await _emailScheduler.Received(1)
            .ScheduleVerificationEmail(updatedUser.Email, updatedUser.Name, token, Arg.Any<string>());
        AssertCookiesContainTokens(tokens.AccessToken, tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task EditUser_ReturnsBadRequest_WhenUsernameIsTaken()
    {
        var currentUser = new User("test_user", "Test User", "test@example.com", "password123")
        {
            Id = 123
        };
        var editUserRequest = new EditUserRequest("New Name", "new_username", "new_user@example.com");
        _userService.GetCurrentUser().Returns(currentUser);
        _userService.EditUser(currentUser.Id, editUserRequest).ThrowsAsync(new UserExistsException(""));

        var result = await _controller.EditUser(editUserRequest, "en");
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username already exists.", badRequestResult.Value);
    }
}
