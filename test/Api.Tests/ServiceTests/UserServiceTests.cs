using Api.Services;
using Api.Tests.Data;
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
}
