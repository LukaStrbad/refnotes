using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using ServerTests.Mocks;
using Server.Services;
using ServerTests.Data;
using ServerTests.Data.Attributes;

namespace ServerTests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
public class UserGroupServiceTests : BaseTests
{
    [Theory, AutoData]
    public async Task CreateGroup_CreatesGroup(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureGroup] UserGroup group)
    {
        var roles = await sut.Context.UserGroupRoles
            .Where(role => role.UserGroupId == group.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(roles);
        var firstRole = roles[0];
        Assert.True(firstRole.UserId == user.Id);
        Assert.Equal(UserGroupRoleType.Owner, firstRole.Role);
    }

    [Theory, AutoData]
    public async Task Update_UpdatesGroup(
        Sut<UserGroupService> sut,
        [FixtureGroup(GroupName = $"test_group_{nameof(Update_UpdatesGroup)}")]
        UserGroup group)
    {
        var rnd = RandomString(32);

        var newName = $"test_group_updated_{rnd}";
        await sut.Value.Update(group.Id, new UpdateGroupDto(newName));

        await sut.Context.Entry(group).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(newName, group.Name);
    }

    [Theory, AutoData]
    public async Task GetUserGroups_ReturnsGroups(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group1,
        [FixtureGroup] UserGroup group2,
        [FixtureGroup] UserGroup group3,
        // Create a group for the second user
        [FixtureGroup(ForUser = nameof(secondUser))]
        UserGroup group4)
    {
        // Check groups for the first user
        var groups = await sut.Value.GetUserGroups();

        var groupIds = groups.Select(g => g.Id).ToList();
        Assert.Equal(3, groups.Count);
        Assert.Contains(group1.Id, groupIds);
        Assert.Contains(group2.Id, groupIds);
        Assert.Contains(group3.Id, groupIds);
        Assert.DoesNotContain(group4.Id, groupIds);
    }

    [Theory, AutoData]
    public async Task AssignRole_AssignsRoles(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Member);

        var roles = await sut.Context.UserGroupRoles
            .Where(role => role.UserGroupId == group.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        var otherUserRole = roles.Single(role => role.UserId == secondUser.Id);
        Assert.Equal(UserGroupRoleType.Member, otherUserRole.Role);

        // Change role
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Admin);

        roles = await sut.Context.UserGroupRoles
            .Where(role => role.UserGroupId == group.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        otherUserRole = roles.Single(role => role.UserId == secondUser.Id);
        Assert.Equal(UserGroupRoleType.Admin, otherUserRole.Role);

        // Try to change to an owner role
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Owner));
    }

    [Theory, AutoData]
    public async Task AssignRole_DemotesAdminIfUserIsOwner(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Admin);
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Member);

        var role = await sut.Context.UserGroupRoles.Where(role =>
                role.UserGroupId == group.Id && role.UserId == secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Theory, AutoData]
    public async Task AssignRole_DemotesSelfIfUserIsAdmin(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        IUserService userService,
        [FixtureGroup] UserGroup group)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Admin);

        // Second user demotes to member
        userService.GetUser().Returns(secondUser);
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Member);

        var role = await sut.Context.UserGroupRoles.Where(role =>
                role.UserGroupId == group.Id && role.UserId == secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Theory, AutoData]
    public async Task AssignRole_ThrowsIfUserIsOwner(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureGroup] UserGroup group)
    {
        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            sut.Value.AssignRole(group.Id, user.Id, UserGroupRoleType.Member));
    }

    [Theory, AutoData]
    public async Task GetGroupMembers_ReturnsMembers(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Member);

        var members = await sut.Value.GetGroupMembers(group.Id);

        var memberIds = members.Select(m => m.Id).ToList();
        Assert.Equal(2, members.Count);
        Assert.Contains(user.Id, memberIds);
        Assert.Contains(secondUser.Id, memberIds);
    }

    [Theory, AutoData]
    public async Task RemoveUser_RemovesUser(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Member);
        await sut.Value.RemoveUser(group.Id, secondUser.Id);

        var members = await sut.Value.GetGroupMembers(group.Id);

        Assert.Single(members);
        Assert.DoesNotContain(members, u => u.Id == secondUser.Id);
    }

    [Theory, AutoData]
    public async Task RemoveUser_ThrowsIfUserIsOwner(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureGroup] UserGroup group)
    {
        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            sut.Value.RemoveUser(group.Id, user.Id));

        var members = await sut.Value.GetGroupMembers(group.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == user.Id);
    }

    [Theory, AutoData]
    public async Task RemoveUser_ThrowsIfUserIsNotPartOfGroup(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Value.RemoveUser(group.Id, secondUser.Id));

        var members = await sut.Value.GetGroupMembers(group.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == user.Id);
    }

    [Theory, AutoData]
    public async Task RemoveUser_RemovesSelf(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group,
        IUserService userService)
    {
        await sut.Value.AssignRole(group.Id, secondUser.Id, UserGroupRoleType.Admin);

        userService.GetUser().Returns(secondUser);
        await sut.Value.RemoveUser(group.Id, secondUser.Id);

        userService.GetUser().Returns(user);
        var members = await sut.Value.GetGroupMembers(group.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == user.Id);
    }

    [Theory, AutoData]
    public async Task GenerateGroupAccessCode_GeneratesCode(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureGroup] UserGroup group)
    {
        var code = await sut.Value.GenerateGroupAccessCode(group.Id, DateTime.UtcNow.AddDays(1));

        var dbCode = await sut.Context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.Value == code, TestContext.Current.CancellationToken);

        Assert.NotEmpty(code);
        Assert.NotNull(dbCode);
    }

    [Theory, AutoData]
    public async Task GenerateGroupAccessCode_ThrowsIfExpiryIsTooLong(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureGroup] UserGroup group)
    {
        // Max expiry is 7 days
        await Assert.ThrowsAsync<ExpiryTimeTooLongException>(() =>
            sut.Value.GenerateGroupAccessCode(group.Id, DateTime.UtcNow.AddDays(8)));

        var dbCodes = await sut.Context.GroupAccessCodes
            .Where(groupCode => groupCode.GroupId == group.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(dbCodes);
    }

    [Theory, AutoData]
    public async Task AddCurrentUserToGroup_AddsUser(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group,
        IUserService userService)
    {
        var code = await sut.Value.GenerateGroupAccessCode(group.Id, DateTime.UtcNow.AddDays(1));

        userService.GetUser().Returns(secondUser);

        await sut.Value.AddCurrentUserToGroup(group.Id, code);

        var members = await sut.Value.GetGroupMembers(group.Id);

        Assert.Contains(members, member => member.Id == secondUser.Id);
    }

    [Theory, AutoData]
    public async Task AddCurrentUserToGroup_ThrowsIfAccessCodeIsInvalid(
        Sut<UserGroupService> sut,
        [FixtureUser] User user,
        [FixtureUser] User secondUser,
        [FixtureGroup] UserGroup group,
        IUserService userService)
    {
        var code = await sut.Value.GenerateGroupAccessCode(group.Id, DateTime.UtcNow.AddDays(1));

        userService.GetUser().Returns(secondUser);

        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            sut.Value.AddCurrentUserToGroup(group.Id, "non-existing-code"));

        var dbCode = await sut.Context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.GroupId == group.Id && groupCode.Value == code,
                TestContext.Current.CancellationToken);
        Assert.NotNull(dbCode);
        dbCode.ExpiryTime = DateTime.UtcNow.AddDays(-1);
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            sut.Value.AddCurrentUserToGroup(group.Id, code));

        userService.GetUser().Returns(user);
        var groupMembers = await sut.Value.GetGroupMembers(group.Id);

        Assert.Single(groupMembers);
    }
}