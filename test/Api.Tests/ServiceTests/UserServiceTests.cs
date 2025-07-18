using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;

namespace Api.Tests.ServiceTests;

public sealed class UserServiceTests
{
    [Theory, AutoData]
    public async Task FindByUsername_ReturnsUser(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user = userFaker.CreateFaker().Generate();

        var foundUser = await sut.Value.GetByUsername(user.Username);

        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
    }

    [Theory, AutoData]
    public async Task EditUser_UpdatesUser(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user = userFaker.CreateFaker().Generate();
        var request = new EditUserRequest("New Name", "new_username", "new_username@example.com");

        var updatedUser = await sut.Value.EditUser(user.Id, request);

        Assert.Equal(user.Id, updatedUser.Id);
        Assert.Equal(request.NewName, updatedUser.Name);
        Assert.Equal(request.NewUsername, updatedUser.Username);
        Assert.Equal(request.NewEmail, updatedUser.Email);
    }

    [Theory, AutoData]
    public async Task EditUser_UpdatesUser_WithSameUsername(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user = userFaker.CreateFaker().Generate();
        var request = new EditUserRequest("New Name", user.Username, "new_username@example.com");
        
        var updatedUser = await sut.Value.EditUser(user.Id, request);
        
        Assert.Equal(user.Id, updatedUser.Id);
        Assert.Equal(request.NewName, updatedUser.Name);
        Assert.Equal(request.NewUsername, updatedUser.Username);
        Assert.Equal(request.NewEmail, updatedUser.Email);
    }

    [Theory, AutoData]
    public async Task EditUser_ThrowException_IfUsernameIsTaken(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user1 = userFaker.CreateFaker().Generate();
        var user2 = userFaker.CreateFaker().Generate();
        var request = new EditUserRequest("New Name", user1.Username, "new_user@example.com");

        await Assert.ThrowsAsync<UserExistsException>(() =>
            sut.Value.EditUser(user2.Id, request));
    }

    [Theory, AutoData]
    public async Task UnconfirmEmail_UnconfirmsEmail(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user = userFaker.CreateFaker().WithConfirmedEmail().Generate();
        
        await sut.Value.UnconfirmEmail(user.Id);

        await sut.Context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.False(user.EmailConfirmed);
    }

    [Theory, AutoData]
    public async Task UpdatePassword_UpdatesUserPassword(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user = userFaker.CreateFaker().Generate();
        var oldPassword = user.Password;
        const string newPassword = "new_secure_password";
        var newCredentials = new UserCredentials(user.Username, newPassword);

        await sut.Value.UpdatePassword(newCredentials);
        
        await sut.Context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(oldPassword, user.Password);
    }
}
