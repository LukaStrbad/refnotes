using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class GroupPermissionServiceTests : BaseTests
{
    private readonly GroupPermissionService _service;
    private readonly IUserGroupService _userGroupService;
    private readonly User _user;

    public GroupPermissionServiceTests()
    {
        var serviceProvider = new ServiceFixture<GroupPermissionService>().WithFakers().CreateServiceProvider();
        _service = serviceProvider.GetRequiredService<GroupPermissionService>();
        _userGroupService = serviceProvider.GetRequiredService<IUserGroupService>();
        _user = serviceProvider.GetRequiredService<FakerResolver>().Get<User>().Generate();
    }

    private void SetGroupRole(int groupId, UserGroupRoleType? role)
        => _userGroupService.GetGroupRoleTypeAsync(groupId, _user.Id).Returns(role);

    private void SetUserGroupRole(int groupId, int userId, UserGroupRoleType? role)
        => _userGroupService.GetGroupRoleTypeAsync(groupId, userId).Returns(role);

    [Fact]
    public async Task HasGroupAccessAsync_ReturnsTrue_WhenUserHasAccess()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Member);

        var hasAccess = await _service.HasGroupAccessAsync(_user, groupId);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task HasGroupAccessAsync_ReturnsFals_WhenUserDoesntHaveAccess()
    {
        const int groupId = 2;
        SetGroupRole(groupId, null);

        var hasAccess = await _service.HasGroupAccessAsync(_user, groupId);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsTrue_WhenUserHasSufficientRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Admin);

        var hasAccess = await _service.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsFalse_WhenUserHasInsufficientRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Member);

        var hasAccess = await _service.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task HasGroupAccessAsync_WithMinRole_ReturnsFalse_WhenUserHasNoRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, null);

        var hasAccess = await _service.HasGroupAccessAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task CanManageRoleAsync_ReturnsTrue_WhenUserHasHigherRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Admin);

        var canManage = await _service.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.True(canManage);
    }

    [Fact]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasSameRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Admin);

        var canManage = await _service.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasLowerRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Member);

        var canManage = await _service.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Admin);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenUserHasNoRole()
    {
        const int groupId = 1;
        SetGroupRole(groupId, null);

        var canManage = await _service.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Member);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageRoleAsync_ReturnsFalse_WhenTargetRoleIsOwner()
    {
        const int groupId = 1;
        SetGroupRole(groupId, UserGroupRoleType.Owner);

        // No role can manage the owner role, including the owner role itself
        var canManage = await _service.CanManageRoleAsync(_user, groupId, UserGroupRoleType.Owner);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsTrue_WhenUserHasHigherRoleThanTarget()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(groupId, targetUserId, UserGroupRoleType.Member);

        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.True(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasSameRoleAsTarget()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(groupId, targetUserId, UserGroupRoleType.Admin);

        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasLowerRoleThanTarget()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, UserGroupRoleType.Member);
        SetUserGroupRole(groupId, targetUserId, UserGroupRoleType.Admin);

        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsFalse_WhenUserHasNoRole()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, null);
        SetUserGroupRole(groupId, targetUserId, UserGroupRoleType.Member);

        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsFalse_WhenTargetUserHasNoRole()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, UserGroupRoleType.Admin);
        SetUserGroupRole(groupId, targetUserId, null);

        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }

    [Fact]
    public async Task CanManageUserAsync_ReturnsFalse_WhenTargetUserIsOwner()
    {
        const int groupId = 1;
        const int targetUserId = 2;

        SetGroupRole(groupId, UserGroupRoleType.Owner);
        SetUserGroupRole(groupId, targetUserId, UserGroupRoleType.Owner);

        // No role can manage the owner role, including the owner role itself
        var canManage = await _service.CanManageUserAsync(_user, groupId, targetUserId);

        Assert.False(canManage);
    }
}
