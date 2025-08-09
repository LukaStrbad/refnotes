using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests;

public sealed class UserServiceTests
{
    private readonly UserService _service;
    private readonly FakerResolver _fakerResolver;

    private readonly User _defaultUser;

    public UserServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<UserService>().WithDb(dbFixture).WithFakers().CreateServiceProvider();
        
        _service = serviceProvider.GetRequiredService<UserService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        
        _defaultUser = _fakerResolver.Get<User>().Generate();
    }

    [Fact]
    public async Task FindByUsername_ReturnsUser()
    {
        var foundUser = await _service.GetByUsername(_defaultUser.Username);

        Assert.NotNull(foundUser);
        Assert.Equal(_defaultUser.Id, foundUser.Id);
    }

    [Fact]
    public async Task EditUser_UpdatesUser()
    {
        var request = new EditUserRequest("New Name", "new_username", "new_username@example.com");

        var updatedUser = await _service.EditUser(_defaultUser.Id, request);

        Assert.Equal(_defaultUser.Id, updatedUser.Id);
        Assert.Equal(request.NewName, updatedUser.Name);
        Assert.Equal(request.NewUsername, updatedUser.Username);
        Assert.Equal(request.NewEmail, updatedUser.Email);
    }

    [Fact]
    public async Task EditUser_UpdatesUser_WithSameUsername()
    {
        var request = new EditUserRequest("New Name", _defaultUser.Username, "new_username@example.com");

        var updatedUser = await _service.EditUser(_defaultUser.Id, request);

        Assert.Equal(_defaultUser.Id, updatedUser.Id);
        Assert.Equal(request.NewName, updatedUser.Name);
        Assert.Equal(request.NewUsername, updatedUser.Username);
        Assert.Equal(request.NewEmail, updatedUser.Email);
    }

    [Fact]
    public async Task EditUser_ThrowException_IfUsernameIsTaken()
    {
        var user1 = _fakerResolver.Get<User>().Generate();
        var user2 = _fakerResolver.Get<User>().Generate();
        var request = new EditUserRequest("New Name", user1.Username, "new_user@example.com");

        await Assert.ThrowsAsync<UserExistsException>(() =>
            _service.EditUser(user2.Id, request));
    }

    [Fact]
    public async Task UnconfirmEmail_UnconfirmsEmail()
    {
        await _service.UnconfirmEmail(_defaultUser.Id);

        Assert.False(_defaultUser.EmailConfirmed);
    }

    [Fact]
    public async Task UpdatePassword_UpdatesUserPassword()
    {
        var oldPassword = _defaultUser.Password;
        const string newPassword = "new_secure_password";
        var newCredentials = new UserCredentials(_defaultUser.Username, newPassword);

        await _service.UpdatePassword(newCredentials);

        Assert.NotEqual(oldPassword, _defaultUser.Password);
    }
}
