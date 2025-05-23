﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ServiceTests;

public class AuthServiceTests : BaseTests
{
    private readonly AuthService _authService;
    private User _user;
    private readonly User _newUser;

    public AuthServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        Context = testDatabaseFixture.CreateContext();
        _authService = new AuthService(Context, AppConfig);
        var rndString = RandomString(32);
        _newUser = new User(0, $"newUser_{rndString}", "newUser", $"new.{rndString}@new.com", DefaultPassword);

        (_user, _) = CreateUser(Context, "test");
    }

    [Fact]
    public async Task Register_ReturnsUser()
    {
        var tokens = await _authService.Register(_newUser);
        var user = await Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username, TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Register_ThrowsExceptionIfUserExists()
    {
        var newUser = new User(0, "test", "test", "test@test.com", DefaultPassword);
        await Assert.ThrowsAsync<UserExistsException>(() => _authService.Register(newUser));
    }

    [Fact]
    public async Task Register_RolesStayEmpty()
    {
        _newUser.Roles = ["admin"];
        await _authService.Register(_newUser);
        var user = await Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username, TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public async Task Login_ReturnsTokens()
    {
        // Don't use _user, because it's password is not hashed
        await _authService.Register(_newUser);
        var tokens = await _authService.Login(new UserCredentials(_newUser.Username, DefaultPassword));

        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken.Token);
    }

    [Fact]
    public async Task Login_ThrowsExceptionIfUserNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _authService.Login(new UserCredentials("nonexistent", DefaultPassword)));
    }

    [Fact]
    public async Task Login_ThrowsExceptionIfPasswordIncorrect()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.Login(new UserCredentials(_user.Username, "incorrect")));
    }

    [Fact]
    public async Task RefreshAccessToken_ReturnsToken()
    {
        var tokens = await _authService.Register(_newUser);
        // Prevents the new token being generated at the same time (tokens would be equal)
        Thread.Sleep(1000);
        var newTokens = await _authService.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token);

        Assert.NotEmpty(newTokens.AccessToken);
        Assert.NotEmpty(newTokens.RefreshToken.Token);
        Assert.NotEqual(tokens.AccessToken, newTokens.AccessToken);
        // In current implementation, refresh tokens are also regenerated
        Assert.NotEqual(tokens.RefreshToken.Token, newTokens.RefreshToken.Token);
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsExceptionIfUserNotFound()
    {
        var tokens = await _authService.Register(_newUser);
        
        // Remove user from database
        var user = await Context.Users.FirstOrDefaultAsync(x => x.Username == _newUser.Username, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Context.Users.Remove(user);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _authService.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken.Token));
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsExceptionIfRefreshTokenInvalid()
    {
        var tokens = await _authService.Register(_newUser);

        await Assert.ThrowsAsync<RefreshTokenInvalid>(() =>
            _authService.RefreshAccessToken(tokens.AccessToken, "invalidRefreshToken"));
    }

    [Fact]
    public async Task RefreshAccessToken_ThrowsAccessTokenInvalid()
    {
        var tokens = await _authService.Register(_newUser);

        await Assert.ThrowsAsync<SecurityTokenMalformedException>(() =>
            _authService.RefreshAccessToken("invalidAccessToken", tokens.RefreshToken.Token));
    }
}