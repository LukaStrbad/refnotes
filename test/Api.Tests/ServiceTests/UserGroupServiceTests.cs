using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class UserGroupServiceTests : BaseTests
{
    private readonly UserGroupService _service;
    private readonly FakerResolver _fakerResolver;
    private readonly RefNotesContext _context;
    private readonly IUserService _userService;

    private readonly User _defaultUser;
    private readonly User _secondUser;
    private readonly UserGroup _defaultGroup;

    public UserGroupServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<UserGroupService>().WithDb(dbFixture).WithFakeEncryption().WithFakers()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<UserGroupService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _userService = serviceProvider.GetRequiredService<IUserService>();

        _defaultUser = _fakerResolver.Get<User>().Generate();
        _secondUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
        _fakerResolver.Get<UserGroupRole>().ForUser(_defaultUser).ForGroup(_defaultGroup)
            .WithRole(UserGroupRoleType.Owner).Generate();
        _userService.GetCurrentUser().Returns(_defaultUser);
    }

    [Fact]
    public async Task Create_CreatesGroup()
    {
        const string groupName = "Test Group";

        var createdGroup = await _service.Create(_defaultUser, groupName);

        var roles = await _context.UserGroupRoles
            .Where(role => role.UserGroupId == createdGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(groupName, createdGroup.Name);
        Assert.Single(roles);
        Assert.Equal(_defaultUser.Id, roles[0].UserId);
        Assert.Equal(UserGroupRoleType.Owner, roles[0].Role);
    }

    [Fact]
    public async Task Update_UpdatesGroup()
    {
        var rnd = RandomString(32);
        var newName = $"test_group_updated_{rnd}";

        await _service.Update(_defaultGroup.Id, new UpdateGroupDto(newName));

        Assert.Equal(newName, _defaultGroup.Name);
    }

    [Fact]
    public async Task GetUserGroups_ReturnsGroups()
    {
        var rolesForDefaultUser = _fakerResolver.Get<UserGroupRole>().ForUser(_defaultUser).Generate(3);
        _fakerResolver.Get<UserGroupRole>().ForUser(_secondUser).Generate(2);
        var defaultUserGroupIds = rolesForDefaultUser.Select(r => r.UserGroupId).Concat([_defaultGroup.Id]).ToList();

        // Check groups for the first user
        var groups = await _service.GetUserGroups();

        var groupIds = groups.Select(g => g.Id).ToList();
        Assert.Equal(defaultUserGroupIds.Count, groups.Count);
        Assert.Equivalent(defaultUserGroupIds, groupIds);
    }

    [Fact]
    public async Task AssignRole_AssignsRoles()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var roles = await _context.UserGroupRoles
            .Where(role => role.UserGroupId == _defaultGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        var otherUserRole = roles.Single(role => role.UserId == _secondUser.Id);
        Assert.Equal(UserGroupRoleType.Member, otherUserRole.Role);

        // Change role
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        roles = await _context.UserGroupRoles
            .Where(role => role.UserGroupId == _defaultGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, roles.Count);
        otherUserRole = roles.Single(role => role.UserId == _secondUser.Id);
        Assert.Equal(UserGroupRoleType.Admin, otherUserRole.Role);

        // Try to change to an owner role
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Owner));
    }

    [Fact]
    public async Task AssignRole_DemotesAdminIfUserIsOwner()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var role = await _context.UserGroupRoles.Where(role =>
                role.UserGroupId == _defaultGroup.Id && role.UserId == _secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Fact]
    public async Task AssignRole_DemotesSelfIfUserIsAdmin()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        // Second user demotes to member
        _userService.GetCurrentUser().Returns(_secondUser);
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var role = await _context.UserGroupRoles.Where(role =>
                role.UserGroupId == _defaultGroup.Id && role.UserId == _secondUser.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Assert that the second user is demoted to a member
        Assert.Equal(UserGroupRoleType.Member, role?.Role);
    }

    [Fact]
    public async Task AssignRole_ThrowsIfUserIsOwner()
    {
        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            _service.AssignRole(_defaultGroup.Id, _defaultUser.Id, UserGroupRoleType.Member));
    }

    [Fact]
    public async Task GetGroupMembers_ReturnsMembers()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Member);

        var members = await _service.GetGroupMembers(_defaultGroup.Id);

        var memberIds = members.Select(m => m.Id).ToList();
        Assert.Equal(2, members.Count);
        Assert.Contains(_defaultUser.Id, memberIds);
        Assert.Contains(_secondUser.Id, memberIds);
    }

    [Fact]
    public async Task RemoveUser_RemovesUser()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Member);
        await _service.RemoveUser(_defaultGroup.Id, _secondUser.Id);

        var members = await _service.GetGroupMembers(_defaultGroup.Id);

        Assert.Single(members);
        Assert.DoesNotContain(members, u => u.Id == _secondUser.Id);
    }

    [Fact]
    public async Task RemoveUser_ThrowsIfUserIsOwner()
    {
        await Assert.ThrowsAsync<UserIsOwnerException>(() =>
            _service.RemoveUser(_defaultGroup.Id, _defaultUser.Id));

        var members = await _service.GetGroupMembers(_defaultGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == _defaultUser.Id);
    }

    [Fact]
    public async Task RemoveUser_ThrowsIfUserIsNotPartOfGroup()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RemoveUser(_defaultGroup.Id, _secondUser.Id));

        var members = await _service.GetGroupMembers(_defaultGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == _defaultUser.Id);
    }

    [Fact]
    public async Task RemoveUser_RemovesSelf()
    {
        await _service.AssignRole(_defaultGroup.Id, _secondUser.Id, UserGroupRoleType.Admin);

        _userService.GetCurrentUser().Returns(_secondUser);
        await _service.RemoveUser(_defaultGroup.Id, _secondUser.Id);

        _userService.GetCurrentUser().Returns(_defaultUser);
        var members = await _service.GetGroupMembers(_defaultGroup.Id);

        Assert.Single(members);
        Assert.Contains(members, u => u.Id == _defaultUser.Id);
    }

    [Fact]
    public async Task GenerateGroupAccessCode_GeneratesCode()
    {
        var code = await _service.GenerateGroupAccessCode(_defaultGroup.Id, DateTime.UtcNow.AddDays(1));

        var dbCode = await _context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.Value == code, TestContext.Current.CancellationToken);

        Assert.NotEmpty(code);
        Assert.NotNull(dbCode);
    }

    [Fact]
    public async Task GenerateGroupAccessCode_ThrowsIfExpiryIsTooLong()
    {
        // Max expiry is 7 days
        await Assert.ThrowsAsync<ExpiryTimeTooLongException>(() =>
            _service.GenerateGroupAccessCode(_defaultGroup.Id, DateTime.UtcNow.AddDays(8)));

        var dbCodes = await _context.GroupAccessCodes
            .Where(groupCode => groupCode.GroupId == _defaultGroup.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(dbCodes);
    }

    [Fact]
    public async Task AddCurrentUserToGroup_AddsUser()
    {
        var code = await _service.GenerateGroupAccessCode(_defaultGroup.Id, DateTime.UtcNow.AddDays(1));
        _userService.GetCurrentUser().Returns(_secondUser);

        await _service.AddCurrentUserToGroup(_defaultGroup.Id, code);

        var members = await _service.GetGroupMembers(_defaultGroup.Id);
        Assert.Contains(members, member => member.Id == _secondUser.Id);
    }

    [Fact]
    public async Task AddCurrentUserToGroup_ThrowsIfAccessCodeIsInvalid()
    {
        var code = await _service.GenerateGroupAccessCode(_defaultGroup.Id, DateTime.UtcNow.AddDays(1));

        _userService.GetCurrentUser().Returns(_secondUser);

        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            _service.AddCurrentUserToGroup(_defaultGroup.Id, "non-existing-code"));

        var dbCode = await _context.GroupAccessCodes
            .FirstOrDefaultAsync(groupCode => groupCode.GroupId == _defaultGroup.Id && groupCode.Value == code,
                TestContext.Current.CancellationToken);
        Assert.NotNull(dbCode);
        dbCode.ExpiryTime = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<AccessCodeInvalidException>(() =>
            _service.AddCurrentUserToGroup(_defaultGroup.Id, code));

        _userService.GetCurrentUser().Returns(_defaultUser);
        var groupMembers = await _service.GetGroupMembers(_defaultGroup.Id);

        Assert.Single(groupMembers);
    }

    [Fact]
    public async Task GetGroupDetailsAsync_ReturnsGroupDetails()
    {
        var groupDetails = await _service.GetGroupDetailsAsync(_defaultGroup.Id);

        Assert.NotNull(groupDetails);
        Assert.Equal(_defaultGroup.Id, groupDetails.Id);
        Assert.Equal(_defaultGroup.Name, groupDetails.Name);
    }
}
