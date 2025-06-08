using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;
using ServerTests.Data;
using ServerTests.Data.Attributes;
using ServerTests.Fixtures;

namespace ServerTests.ServiceTests;

public class AdminServiceTests : BaseTests
{
    [Theory, AutoData]
    public async Task ListUsers_ReturnsUsers(
        Sut<AdminService> sut,
        [FixtureUser] User user,
        [FixtureUser(Roles = ["administrator"])] User adminUser) 
    {
        var users = await sut.Value.ListUsers();
        Assert.True(users.Count >= 2);
        Assert.Contains(users, u => u.Username == adminUser.Username);
        Assert.Contains(users, u => u.Username == user.Username);
    }

    [Theory, AutoData]
    public async Task ModifyRoles_AddsRoles(
        Sut<AdminService> sut,
        [FixtureUser] User user)
    {
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["administrator"], []);
        var roles = await sut.Value.ModifyRoles(modifyRolesRequest);
        Assert.Contains("administrator", roles);
    }

    [Theory, AutoData]
    public async Task ModifyRoles_ThrowsExceptionIfUserNotFound(
        Sut<AdminService> sut)
    {
        var modifyRolesRequest = new ModifyRolesRequest("nonexistent", ["administrator"], []);
        await Assert.ThrowsAsync<UserNotFoundException>(() => sut.Value.ModifyRoles(modifyRolesRequest));
    }

    [Theory, AutoData]
    public async Task ModifyRoles_AddsDistinctRoles(
        Sut<AdminService> sut,
        [FixtureUser] User user)
    {
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["administrator", "administrator"], []);
        var roles = await sut.Value.ModifyRoles(modifyRolesRequest);
        Assert.Single(roles);
        Assert.Contains("administrator", roles);
    }

    [Theory, AutoData]
    public async Task ModifyRoles_RemovesRoles(
        Sut<AdminService> sut,
        [FixtureUser] User user)
    {
        // Add random roles to user
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["random1", "random2"], []);
        await sut.Value.ModifyRoles(modifyRolesRequest);

        modifyRolesRequest = new ModifyRolesRequest(user.Username, [], ["random1"]);
        var roles = await sut.Value.ModifyRoles(modifyRolesRequest);
        Assert.DoesNotContain("random1", roles);
        Assert.Contains("random2", roles);

        var userInDb = await sut.Context.Users.FirstOrDefaultAsync(u => u.Username == user.Username,
            TestContext.Current.CancellationToken);
        Assert.NotNull(userInDb);
        Assert.Equal(roles, userInDb.Roles);
    }
}