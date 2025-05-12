using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ServiceTests;

public class AdminServiceTests : BaseTests, IDisposable
{
    private readonly AdminService _adminService;
    private readonly User _adminUser;
    private readonly User _user;

    public AdminServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        Context = testDatabaseFixture.CreateContext();
        _adminService = new AdminService(Context);

        var rnd = RandomString(32);
        (_adminUser, _) = CreateUser(Context, $"admin{rnd}", "admininstrator");
        (_user, _) = CreateUser(Context, $"test{rnd}");
    }

    [Fact]
    public async Task ListUsers_ReturnsUsers()
    {
        var users = await _adminService.ListUsers();
        Assert.True(users.Count >= 2);
        Assert.Contains(users, u => u.Username == _adminUser.Username);
        Assert.Contains(users, u => u.Username == _user.Username);
    }

    [Fact]
    public async Task ModifyRoles_AddsRoles()
    {
        var modifyRolesRequest = new ModifyRolesRequest(_user.Username, ["administrator"], []);
        var roles = await _adminService.ModifyRoles(modifyRolesRequest);
        Assert.Contains("administrator", roles);
    }

    [Fact]
    public async Task ModifyRoles_ThrowsExceptionIfUserNotFound()
    {
        var modifyRolesRequest = new ModifyRolesRequest("nonexistent", ["administrator"], []);
        await Assert.ThrowsAsync<UserNotFoundException>(() => _adminService.ModifyRoles(modifyRolesRequest));
    }

    [Fact]
    public async Task ModifyRoles_AddsDistinctRoles()
    {
        var modifyRolesRequest = new ModifyRolesRequest(_user.Username, ["administrator", "administrator"], []);
        var roles = await _adminService.ModifyRoles(modifyRolesRequest);
        Assert.Single(roles);
        Assert.Contains("administrator", roles);
    }

    [Fact]
    public async Task ModifyRoles_RemovesRoles()
    {
        // Add random roles to user
        var modifyRolesRequest = new ModifyRolesRequest(_user.Username, ["random1", "random2"], []);
        await _adminService.ModifyRoles(modifyRolesRequest);

        modifyRolesRequest = new ModifyRolesRequest(_user.Username, [], ["random1"]);
        var roles = await _adminService.ModifyRoles(modifyRolesRequest);
        Assert.DoesNotContain("random1", roles);
        Assert.Contains("random2", roles);

        var userInDb = await Context.Users.FirstOrDefaultAsync(u => u.Username == _user.Username,
            TestContext.Current.CancellationToken);
        Assert.NotNull(userInDb);
        Assert.Equal(roles, userInDb.Roles);
    }
}