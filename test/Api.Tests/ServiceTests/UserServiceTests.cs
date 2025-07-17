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
    public async Task EditUser_ThrowException_IfUsernameIsTaken(Sut<UserService> sut, UserFakerImplementation userFaker)
    {
        var user1 = userFaker.CreateFaker().Generate();
        var user2 = userFaker.CreateFaker().Generate();
        var request = new EditUserRequest("New Name", user1.Username, "new_user@example.com");

        await Assert.ThrowsAsync<UserExistsException>(() =>
            sut.Value.EditUser(user2.Id, request));
    }
}
