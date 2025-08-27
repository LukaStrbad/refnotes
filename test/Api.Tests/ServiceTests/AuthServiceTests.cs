using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests.ServiceTests;

public class AuthServiceTests : BaseTests
{
    private readonly RegisterUserRequest _newUser;
    private readonly AuthService _service;
    private readonly RefNotesContext _context;
    private readonly FakerResolver _fakerResolver;

    private const string DefaultPassword = "password";

    public AuthServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<AuthService>().WithDb(dbFixture).WithFakers().CreateServiceProvider();
        _service = serviceProvider.GetRequiredService<AuthService>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();

        var rndString = RandomString(32);
        _newUser = new RegisterUserRequest($"newUser_{rndString}", "newUser", $"new.{rndString}@new.com", DefaultPassword);
    }

    [Fact]
    public async Task Register_ReturnsUser()
    {
        // Act
        var tokens = await _service.Register(_newUser);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username, TestContext.Current.CancellationToken);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Register_ThrowsExceptionIfUserExists()
    {
        // Arrange
        var existingUser = _fakerResolver.Get<User>().Generate();
        var newUser = new RegisterUserRequest(existingUser.Username, existingUser.Name, existingUser.Email, "Password123");

        // Act/Assert
        await Assert.ThrowsAsync<UserExistsException>(() => _service.Register(newUser));
    }

    [Fact]
    public async Task Login_ReturnsTokens()
    {
        // Arrange
        await _service.Register(_newUser);

        // Act
        var tokens = await _service.Login(new UserCredentials(_newUser.Username, DefaultPassword));

        // Assert
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Login_ThrowsExceptionIfUserNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _service.Login(new UserCredentials("nonexistent", DefaultPassword)));
    }

    [Fact]
    public async Task Login_ThrowsExceptionIfPasswordIncorrect()
    {
        // Arrange
        var tokens = await _service.Register(_newUser);

        // Act/Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.Login(new UserCredentials(_newUser.Username,"incorrect")));
    }

    [Fact]
    public async Task RefreshAccessToken_ReturnsToken()
    {
        // Arrange
        var tokens = await _service.Register(_newUser);

        // Act
        await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait for 1 second to ensure that the refresh token is different
        var newTokens = await _service.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token);

        // Assert
        Assert.NotEmpty(newTokens.AccessToken);
        Assert.NotEmpty(newTokens.RefreshToken.Token);
        Assert.NotEqual(tokens.AccessToken, newTokens.AccessToken);
        Assert.NotEqual(tokens.RefreshToken.Token, newTokens.RefreshToken.Token);
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsExceptionIfUserNotFound()
    {
        // Arrange
        var tokens = await _service.Register(_newUser);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act/Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _service.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token));
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsExceptionIfRefreshTokenInvalid()
    {
        // Arrange
        var tokens = await _service.Register(_newUser);

        // Act/Assert
        await Assert.ThrowsAsync<RefreshTokenInvalid>(() =>
            _service.RefreshAccessToken(tokens.AccessToken, "invalidRefreshToken"));
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsAccessTokenInvalid()
    {
        // Arrange
        var tokens = await _service.Register(_newUser);

        // Act/Assert
        await Assert.ThrowsAsync<SecurityTokenMalformedException>(() =>
            _service.RefreshAccessToken("invalidAccessToken", tokens.RefreshToken.Token));
    }
}
