using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests;

public class AdminServiceTests : BaseTests
{
    private readonly AdminService _service;
    private readonly FakerResolver _fakerResolver;

    public AdminServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<AdminService>()
            .WithDb(dbFixture)
            .WithFakers()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<AdminService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
    }

    [Fact]
    public async Task ListUsers_ReturnsUsers()
    {
        // Arrange
        var user = _fakerResolver.Get<User>().Generate();
        var adminUser = _fakerResolver.Get<User>().WithRoles("administrator").Generate();

        // Act
        var users = await _service.ListUsers();

        // Assert
        Assert.True(users.Count >= 2);
        Assert.Contains(users, u => u.Username == adminUser.Username);
        Assert.Contains(users, u => u.Username == user.Username);
    }

    [Fact]
    public async Task ModifyRoles_AddsRoles()
    {
        // Arrange
        var user = _fakerResolver.Get<User>().Generate();
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["administrator"], []);

        // Act
        var roles = await _service.ModifyRoles(modifyRolesRequest);

        // Assert
        Assert.Contains("administrator", roles);
        Assert.Equal(["administrator"], user.Roles);
    }

    [Fact]
    public async Task ModifyRoles_ThrowsExceptionIfUserNotFound()
    {
        // Arrange
        var modifyRolesRequest = new ModifyRolesRequest("nonexistent", ["administrator"], []);

        // Act/Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _service.ModifyRoles(modifyRolesRequest));
    }

    [Fact]
    public async Task ModifyRoles_AddsDistinctRoles()
    {
        // Arrange
        var user = _fakerResolver.Get<User>().Generate();
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["administrator", "administrator"], []);

        // Act
        var roles = await _service.ModifyRoles(modifyRolesRequest);

        // Assert
        Assert.Single(roles);
        Assert.Contains("administrator", roles);
        Assert.Equal(["administrator"], user.Roles);
    }

    [Fact]
    public async Task ModifyRoles_RemovesRoles()
    {
        // Arrange
        var user = _fakerResolver.Get<User>().Generate();
        // Add random roles to user
        var modifyRolesRequest = new ModifyRolesRequest(user.Username, ["random1", "random2"], []);
        await _service.ModifyRoles(modifyRolesRequest);

        // Act
        modifyRolesRequest = new ModifyRolesRequest(user.Username, [], ["random1"]);
        var roles = await _service.ModifyRoles(modifyRolesRequest);

        // Assert
        Assert.DoesNotContain("random1", roles);
        Assert.Contains("random2", roles);
        Assert.Equal(["random2"], user.Roles);
    }
}
