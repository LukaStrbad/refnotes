using Api.Services;
using Api.Tests.Data;
using Data.Model;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class GroupPermissionServiceTests : BaseTests
{
    private readonly User _user = new(1, "user", "User", "user@mail.com", "password");

    private void SetGroupRole(Sut<GroupPermissionService> sut, int groupId, UserGroupRoleType? role)
    {
        sut.ServiceProvider.GetRequiredService<IUserGroupService>()
            .GetGroupRoleTypeAsync(groupId, _user.Id)
            .Returns(role);
    }

    private static void SetUserGroupRole(Sut<GroupPermissionService> sut, int groupId, int userId,
        UserGroupRoleType? role)
    {
        sut.ServiceProvider.GetRequiredService<IUserGroupService>()
            .GetGroupRoleTypeAsync(groupId, userId)
            .Returns(role);
    }

    [Theory, AutoData]
    public async Task HasGroupAccessAsync_ReturnsTrue_WhenUserHasAccess(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Member);

        var hasAccess = await sut.Value.HasGroupAccessAsync(_user, groupId);

        Assert.True(hasAccess);
    }

    [Theory, AutoData]
    public async Task HasGroupAccessAsync_ReturnsFals_WhenUserDoesntHaveAccess(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 2;
        SetGroupRole(sut, groupId, null);

        var hasAccess = await sut.Value.HasGroupAccessAsync(_user, groupId);

        Assert.False(hasAccess);
    }

    [Theory, AutoData]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsTrue_WhenUserHasSufficientRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);

        var hasAccess = await sut.Value.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.True(hasAccess);
    }

    [Theory, AutoData]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsFalse_WhenUserHasInsufficientRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Member);

        var hasAccess = await sut.Value.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(hasAccess);
    }

    [Theory, AutoData]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsFalse_WhenUserHasNoRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, null);

        var hasAccess = await sut.Value.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.False(hasAccess);
    }

    [Theory, AutoData]
    public async Task CanManageRoleAsync_ReturnsTrue_WhenUserHasHigherRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);

        var canManage = await sut.Value.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.True(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasSameRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);

        var canManage = await sut.Value.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasLowerRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Member);

        var canManage = await sut.Value.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasNoRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, null);

        var canManage = await sut.Value.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenTargetRoleIsOwner(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        SetGroupRole(sut, groupId, UserGroupRoleType.Owner);

        // No role can manage the owner role, including the owner role itself
        var canManage = await sut.Value.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Owner);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsTrue_WhenUserHasHigherRoleThanTarget(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(sut, groupId, targetUserId, UserGroupRoleType.Member);

        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.True(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasSameRoleAsTarget(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(sut, groupId, targetUserId, UserGroupRoleType.Admin);

        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasLowerRoleThanTarget(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, UserGroupRoleType.Member);
        SetUserGroupRole(sut, groupId, targetUserId, UserGroupRoleType.Admin);

        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasNoRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, null);
        SetUserGroupRole(sut, groupId, targetUserId, UserGroupRoleType.Member);

        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsFalse_WhenTargetUserHasNoRole(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(sut, groupId, targetUserId, null);

        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Theory, AutoData]
    public async Task CanManageUserAsync_ReturnsFalse_WhenTargetUserIsOwner(
        Sut<GroupPermissionService> sut)
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(sut, groupId, UserGroupRoleType.Owner);
        SetUserGroupRole(sut, groupId, targetUserId, UserGroupRoleType.Owner);

        // No role can manage the owner role, including the owner role itself
        var canManage = await sut.Value.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }
}
