using Microsoft.EntityFrameworkCore;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using ServerTests.Mocks;
using Server.Services;
using ServerTests.Fixtures;

namespace ServerTests.ServiceTests;

[Trait("Category", "Group")]
public class UserGroupServiceTests : BaseTests
{
    private readonly User _user;
    private readonly User _secondUser;
    private readonly User _thirdUser;
    private readonly UserGroupService _userGroupService;

    public UserGroupServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        Context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_user, _) = CreateUser(Context, $"test_{rndString}");
        (_secondUser, _) = CreateUser(Context, $"test_second_{rndString}");
        (_thirdUser, _) = CreateUser(Context, $"test_third_{rndString}");

        var encryptionService = new FakeEncryptionService();
        SetUser(_user);

        _userGroupService = new UserGroupService(Context, encryptionService, UserService);
    }

    [Fact]
    public async Task CreateGroup_CreatesGroup()
    {
        var dbGroup = await CreateRandomGroup();

        var roles = await Context.UserGroupRoles
            .Where(role => role.UserGroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(roles);
        var firstRole = roles[0];
        Assert.True(firstRole.UserId == _user.Id);
        Assert.Equal(UserGroupRoleType.Owner, firstRole.Role);
    }


    [Fact]
    public async Task Update_UpdatesGroup()
    {
        var rnd = RandomString(32);
        var initialName = $"test_group_{rnd}";
        var dbGroup = await CreateRandomGroup(initialName);

        var newName = $"test_group_updated_{rnd}";
        await _userGroupService.Update(dbGroup.Id, new UpdateGroupDto(newName));

        await Context.Entry(dbGroup).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(newName, dbGroup.Name);
    }

    [Fact]
    public async Task GetUserGroups_ReturnsGroups()
    {
        var group1 = await CreateRandomGroup();
        var group2 = await CreateRandomGroup();
        var group3 = await CreateRandomGroup();

        // Create a group as another user
        SetUser(_secondUser);
        var group4 = await CreateRandomGroup();

        // Check groups for the first user
        SetUser(_user);
        var groups = await _userGroupService.GetUserGroups();

        var groupIds = groups.Select(g => g.Id).ToList();
        Assert.Equal(3, groups.Count);
        Assert.Contains(group1.Id, groupIds);
        Assert.Contains(group2.Id, groupIds);
        Assert.Contains(group3.Id, groupIds);
        Assert.DoesNotContain(group4.Id, groupIds);
    }

    [Fact]
    public async Task AssignRole_AssignsRoles()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var roles = await Context.UserGroupRoles
            .Where(role => role.UserGroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        var otherUserRole = roles.Single(role => role.UserId == _secondUser.Id);
        Assert.Equal(UserGroupRoleType.Member, otherUserRole.Role);

        // Change role
        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        roles = await Context.UserGroupRoles
            .Where(role => role.UserGroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        otherUserRole = roles.Single(role => role.UserId == _secondUser.Id);
        Assert.Equal(UserGroupRoleType.Admin, otherUserRole.Role);

        // Try to change to an owner role
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Owner));
    }

    [Fact]
    public async Task AssignRole_DemotesAdminIfUserIsOwner()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);
        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var role = await Context.UserGroupRoles.Where(role =>
                role.UserGroupId == dbGroup.Id && role.UserId == _secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Fact]
    public async Task AssignRole_DemotesSelfIfUserIsAdmin()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        // Second user demotes to member
        SetUser(_secondUser);
        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var role = await Context.UserGroupRoles.Where(role =>
                role.UserGroupId == dbGroup.Id && role.UserId == _secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Fact]
    public async Task AssignRole_ThrowsIfUserIsOwner()
    {
        var dbGroup = await CreateRandomGroup();

        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            _userGroupService.AssignRole(dbGroup.Id, _user.Id, UserGroupRoleType.Member));
    }

    [Fact]
    public async Task GetGroupMembers_ReturnsMembers()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        var memberIds = members.Select(m => m.Id).ToList();
        Assert.Equal(2, members.Count);
        Assert.Contains(_user.Id, memberIds);
        Assert.Contains(_secondUser.Id, memberIds);
    }

    [Fact]
    public async Task RemoveUser_RemovesUser()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Member);
        await _userGroupService.RemoveUser(dbGroup.Id, _secondUser.Id);

        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Single(members);
        Assert.DoesNotContain(members, user => user.Id == _secondUser.Id);
    }

    [Fact]
    public async Task RemoveUser_ThrowsIfUserIsOwner()
    {
        var dbGroup = await CreateRandomGroup();

        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            _userGroupService.RemoveUser(dbGroup.Id, _user.Id));

        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, user => user.Id == _user.Id);
    }

    [Fact]
    public async Task RemoveUser_ThrowsIfUserIsNotPartOfGroup()
    {
        var dbGroup = await CreateRandomGroup();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userGroupService.RemoveUser(dbGroup.Id, _secondUser.Id));

        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, user => user.Id == _user.Id);
    }

    [Fact]
    public async Task RemoveUser_RemovesSelf()
    {
        var dbGroup = await CreateRandomGroup();

        await _userGroupService.AssignRole(dbGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        SetUser(_secondUser);
        await _userGroupService.RemoveUser(dbGroup.Id, _secondUser.Id);

        SetUser(_user);
        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, user => user.Id == _user.Id);
    }

    [Fact]
    public async Task GenerateGroupAccessCode_GeneratesCode()
    {
        var dbGroup = await CreateRandomGroup();

        var code = await _userGroupService.GenerateGroupAccessCode(dbGroup.Id, DateTime.UtcNow.AddDays(1));

        var dbCode = await Context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.Value == code, TestContext.Current.CancellationToken);

        Assert.NotEmpty(code);
        Assert.NotNull(dbCode);
    }

    [Fact]
    public async Task GenerateGroupAccessCode_ThrowsIfExpiryIsTooLong()
    {
        var dbGroup = await CreateRandomGroup();

        // Max expiry is 7 days
        await Assert.ThrowsAsync<ExpiryTimeTooLongException>(() =>
            _userGroupService.GenerateGroupAccessCode(dbGroup.Id, DateTime.UtcNow.AddDays(8)));

        var dbCodes = await Context.GroupAccessCodes
            .Where(groupCode => groupCode.GroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(dbCodes);
    }

    [Fact]
    public async Task AddCurrentUserToGroup_AddsUser()
    {
        var dbGroup = await CreateRandomGroup();
        var code = await _userGroupService.GenerateGroupAccessCode(dbGroup.Id, DateTime.UtcNow.AddDays(1));

        SetUser(_secondUser);

        await _userGroupService.AddCurrentUserToGroup(dbGroup.Id, code);

        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Contains(members, member => member.Id == _secondUser.Id);
    }

    [Fact]
    public async Task AddCurrentUserToGroup_ThrowsIfAccessCodeIsInvalid()
    {
        var dbGroup = await CreateRandomGroup();
        var code = await _userGroupService.GenerateGroupAccessCode(dbGroup.Id, DateTime.UtcNow.AddDays(1));

        SetUser(_secondUser);

        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            _userGroupService.AddCurrentUserToGroup(dbGroup.Id, "non-existing-code"));

        var dbCode = await Context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.GroupId == dbGroup.Id && groupCode.Value == code,
                TestContext.Current.CancellationToken);
        Assert.NotNull(dbCode);
        dbCode.ExpiryTime = DateTime.UtcNow.AddDays(-1);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            _userGroupService.AddCurrentUserToGroup(dbGroup.Id, code));

        SetUser(_user);
        var groupMembers = await _userGroupService.GetGroupMembers(dbGroup.Id);

        Assert.Single(groupMembers);
    }
}