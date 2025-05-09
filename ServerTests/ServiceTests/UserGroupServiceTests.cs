using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using ServerTests.Mocks;
using Server.Services;
using Server.Utils;

namespace ServerTests.ServiceTests;

public class UserGroupServiceTests : BaseTests
{
    private readonly RefNotesContext _context;
    private readonly User _user;
    private readonly User _otherUser;
    private readonly UserGroupService _userGroupService;
    private readonly IServiceUtils _serviceUtils;

    public UserGroupServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        _context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_user, _) = CreateUser(_context, $"test_{rndString}");
        (_otherUser, _) = CreateUser(_context, $"test_other_{rndString}");

        var encryptionService = new FakeEncryptionService();
        _serviceUtils = Substitute.For<IServiceUtils>();
        _serviceUtils.GetUser().Returns(_user);

        _userGroupService = new UserGroupService(_context, encryptionService, _serviceUtils);
    }

    private async Task<UserGroup> CreateRandomGroup(string? groupName = null)
    {
        if (groupName is null)
        {
            var rnd = RandomString(32);
            groupName = $"test_group_{rnd}";
        }

        await _userGroupService.Create(groupName);

        var dbGroup = await _context.UserGroups
            .Where(group => group.Name == groupName)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(dbGroup);
        return dbGroup;
    }

    [Fact]
    public async Task CreateGroup_CreatesGroup()
    {
        var dbGroup = await CreateRandomGroup();

        var roles = await _context.UserGroupRoles
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
        await _userGroupService.Update(new UpdateGroupDto(dbGroup.Id, newName));

        await _context.Entry(dbGroup).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(newName, dbGroup.Name);
    }

    [Fact]
    public async Task GetUserGroups_ReturnsGroups()
    {
        var group1 = await CreateRandomGroup();
        var group2 = await CreateRandomGroup();
        var group3 = await CreateRandomGroup();

        // Create a group as another user
        _serviceUtils.GetUser().Returns(_otherUser);
        var group4 = await CreateRandomGroup();

        // Check groups for the first user
        _serviceUtils.GetUser().Returns(_user);
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

        await _userGroupService.AssignRole(dbGroup.Id, _otherUser.Id, UserGroupRoleType.Member);

        var roles = await _context.UserGroupRoles
            .Where(role => role.UserGroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        var otherUserRole = roles.Single(role => role.UserId == _otherUser.Id);
        Assert.Equal(UserGroupRoleType.Member, otherUserRole.Role);

        // Change role
        await _userGroupService.AssignRole(dbGroup.Id, _otherUser.Id, UserGroupRoleType.Admin);

        roles = await _context.UserGroupRoles
            .Where(role => role.UserGroupId == dbGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        otherUserRole = roles.Single(role => role.UserId == _otherUser.Id);
        Assert.Equal(UserGroupRoleType.Admin, otherUserRole.Role);

        // Try to change to an owner role
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userGroupService.AssignRole(dbGroup.Id, _otherUser.Id, UserGroupRoleType.Owner));
    }

    [Fact]
    public async Task AssignRole_ThrowsIfUserIsOwner()
    {
        var dbGroup = await CreateRandomGroup();
        
        await Assert.ThrowsAsync<UserIsOwnerException>(() => 
            _userGroupService.AssignRole(dbGroup.Id, _user.Id, UserGroupRoleType.Member));
    }

    [Fact]
    public async Task AssignRole_ThrowsIfUserNotInGroup()
    {
        var dbGroup = await CreateRandomGroup();
        
        _serviceUtils.GetUser().Returns(_otherUser);
        
        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _userGroupService.AssignRole(dbGroup.Id, _otherUser.Id, UserGroupRoleType.Member));
    }

    [Fact]
    public async Task GetGroupMembers_ReturnsMembers()
    {
        var dbGroup = await CreateRandomGroup();
        
        await _userGroupService.AssignRole(dbGroup.Id, _otherUser.Id, UserGroupRoleType.Member);
        
        var members = await _userGroupService.GetGroupMembers(dbGroup.Id);
        
        var memberIds = members.Select(m => m.Id).ToList();
        Assert.Equal(2, members.Count);
        Assert.Contains(_user.Id, memberIds);
        Assert.Contains(_otherUser.Id, memberIds);
    }

    [Fact]
    public async Task GetGroupMembers_ThrowsIfUserNotInGroup()
    {
        var dbGroup = await CreateRandomGroup();
        
        _serviceUtils.GetUser().Returns(_otherUser);
        
        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _userGroupService.GetGroupMembers(dbGroup.Id));
    }
}