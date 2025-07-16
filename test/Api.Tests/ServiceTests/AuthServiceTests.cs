using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests.ServiceTests;

public class AuthServiceTests : BaseTests
{
    private readonly User _newUser;

    private const string DefaultPassword = "password";

    public AuthServiceTests()
    {
        var rndString = RandomString(32);
        _newUser = new User(0, $"newUser_{rndString}", "newUser", $"new.{rndString}@new.com", DefaultPassword);
    }

    [Theory, AutoData]
    public async Task Register_ReturnsUser(Sut<AuthService> sut)
    {
        var tokens = await sut.Value.Register(_newUser);
        var user = await sut.Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username,
            TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Theory, AutoData]
    public async Task Register_ThrowsExceptionIfUserExists(Sut<AuthService> sut, [FixtureUser] User existingUser)
    {
        var newUser = new User(0, existingUser.Username, existingUser.Name, existingUser.Email, "Password123");
        await Assert.ThrowsAsync<UserExistsException>(() => sut.Value.Register(newUser));
    }

    [Theory, AutoData]
    public async Task Register_RolesStayEmpty(Sut<AuthService> sut)
    {
        _newUser.Roles = ["admin"];
        await sut.Value.Register(_newUser);
        var user = await sut.Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username,
            TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Empty(user.Roles);
    }

    [Theory, AutoData]
    public async Task Login_ReturnsTokens(Sut<AuthService> sut)
    {
        await sut.Value.Register(_newUser);
        var tokens = await sut.Value.Login(new UserCredentials(_newUser.Username, DefaultPassword));

        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Theory, AutoData]
    public async Task Login_ThrowsExceptionIfUserNotFound(Sut<AuthService> sut)
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            sut.Value.Login(new UserCredentials("nonexistent", DefaultPassword)));
    }

    [Theory, AutoData]
    public async Task Login_ThrowsExceptionIfPasswordIncorrect(Sut<AuthService> sut, [FixtureUser] User user)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.Value.Login(new UserCredentials(user.Username, "incorrect")));
    }

    [Theory, AutoData]
    public async Task RefreshAccessToken_ReturnsToken(Sut<AuthService> sut)
    {
        var tokens = await sut.Value.Register(_newUser);
        // Prevents the new token being generated at the same time (tokens would be equal)
        Thread.Sleep(1000);
        var newTokens = await sut.Value.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token);

        Assert.NotEmpty(newTokens.AccessToken);
        Assert.NotEmpty(newTokens.RefreshToken.Token);
        Assert.NotEqual(tokens.AccessToken, newTokens.AccessToken);
        // In the current implementation, refresh tokens are also regenerated
        Assert.NotEqual(tokens.RefreshToken.Token, newTokens.RefreshToken.Token);
    }

    [Theory, AutoData]
    public async Task RefreshAccessToken_ThrowsExceptionIfUserNotFound(Sut<AuthService> sut)
    {
        var tokens = await sut.Value.Register(_newUser);

        // Remove user from database
        var user = await sut.Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username,
            TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        sut.Context.Users.Remove(user);
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            sut.Value.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token));
    }

    [Theory, AutoData]
    public async Task RefreshAccessToken_ThrowsExceptionIfRefreshTokenInvalid(Sut<AuthService> sut)
    {
        var tokens = await sut.Value.Register(_newUser);

        await Assert.ThrowsAsync<RefreshTokenInvalid>(() =>
            sut.Value.RefreshAccessToken(tokens.AccessToken, "invalidRefreshToken"));
    }

    [Theory, AutoData]
    public async Task RefreshAccessToken_ThrowsAccessTokenInvalid(Sut<AuthService> sut)
    {
        var tokens = await sut.Value.Register(_newUser);

        await Assert.ThrowsAsync<SecurityTokenMalformedException>(() =>
            sut.Value.RefreshAccessToken("invalidAccessToken", tokens.RefreshToken.Token));
    }
}
