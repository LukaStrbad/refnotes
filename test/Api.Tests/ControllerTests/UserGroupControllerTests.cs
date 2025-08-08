using Api.Controllers;
using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.ControllerTests;

[Trait("Category", "Group")]
public class UserGroupControllerTests : IClassFixture<ServiceFixture<UserGroupController>>
{
    private readonly IUserGroupService _userGroupService;
    private readonly IGroupPermissionService _groupPermissionService;
    private readonly UserGroupController _controller;

    public UserGroupControllerTests(ServiceFixture<UserGroupController> fixture)
    {
        var serviceProvider = fixture.CreateServiceProvider();
        _userGroupService = serviceProvider.GetRequiredService<IUserGroupService>();
        _groupPermissionService = serviceProvider.GetRequiredService<IGroupPermissionService>();
        _controller = serviceProvider.GetRequiredService<UserGroupController>();
    }

    [Fact]
    public async Task Create_ReturnsOk_WithId()
    {
        var createdGroup = new GroupDto(123, "TestGroup", UserGroupRoleType.Owner);
        _userGroupService.Create(Arg.Any<string>()).Returns(createdGroup);

        var result = await _controller.Create("TestGroup");
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(createdGroup, okResult.Value);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var dto = new UpdateGroupDto("New name");
        const int groupId = 10;

        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), groupId, UserGroupRoleType.Admin)
            .Returns(true);

        var result = await _controller.Update(groupId, dto);
        var okResult = Assert.IsType<OkResult>(result);

        await _userGroupService.Received(1).Update(groupId, dto);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetUserGroups_ReturnsOk_WithGroups()
    {
        var groups = new List<GroupDto> { new(1, "Test Group", UserGroupRoleType.Member) };
        _userGroupService.GetUserGroups().Returns(groups);

        var result = await _controller.GetUserGroups();
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(groups, okResult.Value);
    }

    [Fact]
    public async Task GetGroupMembers_ReturnsOk_WithMembers()
    {
        var members = new List<GroupUserDto> { new(1, "jdoe", "John Doe", UserGroupRoleType.Member) };
        _userGroupService.GetGroupMembers(5).Returns(members);
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), 5).Returns(true);

        var result = await _controller.GetGroupMembers(5);
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(members, okResult.Value);
    }

    [Fact]
    public async Task AssignRole_ReturnsOk()
    {
        var dto = new AssignRoleDto(1, UserGroupRoleType.Admin);
        _groupPermissionService.CanManageRoleAsync(Arg.Any<User>(), 1, UserGroupRoleType.Admin).Returns(true);

        var result = await _controller.AssignRole(1, dto);
        var okResult = Assert.IsType<OkResult>(result);

        await _userGroupService.Received(1).AssignRole(1, dto.UserId, dto.Role);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AssignRole_ReturnsBadRequest_ForInvalidOperationException()
    {
        const int groupId = 5;
        var dto = new AssignRoleDto(2, UserGroupRoleType.Member);
        _userGroupService.AssignRole(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UserGroupRoleType>())
            .ThrowsAsync(new InvalidOperationException("error"));
        _groupPermissionService.CanManageRoleAsync(Arg.Any<User>(), groupId, UserGroupRoleType.Member).Returns(true);

        var result = await _controller.AssignRole(groupId, dto);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_ForUserNotFoundException()
    {
        const int groupId = 5;
        var dto = new AssignRoleDto(99, UserGroupRoleType.Member);
        _userGroupService.AssignRole(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UserGroupRoleType>())
            .ThrowsAsync(new UserNotFoundException("not found"));
        _groupPermissionService.CanManageRoleAsync(Arg.Any<User>(), groupId, UserGroupRoleType.Member).Returns(true);

        var result = await _controller.AssignRole(groupId, dto);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.NotNull(notFoundResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("not found", notFoundResult.Value);
    }

    [Fact]
    public async Task RemoveUser_ReturnsOk()
    {
        const int groupId = 2;
        const int userId = 3;
        _groupPermissionService.CanManageUserAsync(Arg.Any<User>(), groupId, userId).Returns(true);

        var result = await _controller.RemoveUser(groupId, userId);
        var okResult = Assert.IsType<OkResult>(result);

        await _userGroupService.Received(1).RemoveUser(groupId, userId);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ReturnsBadRequest_ForInvalidOperationException()
    {
        const int groupId = 1;
        const int userId = 1;
        _userGroupService.RemoveUser(Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(new InvalidOperationException("fail"));
        _groupPermissionService.CanManageUserAsync(Arg.Any<User>(), groupId, userId).Returns(true);

        var result = await _controller.RemoveUser(groupId, userId);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
        Assert.Equal("fail", badResult.Value);
    }

    [Fact]
    public async Task GenerateAccessCode_ReturnsOk()
    {
        _userGroupService.GenerateGroupAccessCode(1, Arg.Any<DateTime>()).Returns("CODE123");
        _groupPermissionService.HasGroupAccessAsync(Arg.Any<User>(), 1, UserGroupRoleType.Admin).Returns(true);

        var expiry = DateTime.UtcNow.AddDays(3);
        var result = await _controller.GenerateAccessCode(1, expiry);
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal("CODE123", okResult.Value);
    }

    [Fact]
    public async Task AddCurrentUserWithCode_ReturnsOk()
    {
        var result = await _controller.AddCurrentUserWithCode(5, "code");
        var okResult = Assert.IsType<OkResult>(result);

        await _userGroupService.Received(1).AddCurrentUserToGroup(5, "code");
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AddCurrentUserWithCode_ReturnsBadRequest_ForInvalidOperationException()
    {
        _userGroupService.AddCurrentUserToGroup(Arg.Any<int>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("bad code"));

        var result = await _controller.AddCurrentUserWithCode(7, "bad");
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
        Assert.Equal("bad code", badResult.Value);
    }

    [Fact]
    public async Task AddCurrentUserWithCode_ReturnsForbid_ForAccessCodeInvalidException()
    {
        _userGroupService.AddCurrentUserToGroup(Arg.Any<int>(), Arg.Any<string>())
            .ThrowsAsync(new AccessCodeInvalidException("forbidden"));

        var result = await _controller.AddCurrentUserWithCode(7, "bad");
        var forbidResult = Assert.IsType<ForbidResult>(result);

        Assert.NotNull(forbidResult);
    }
}
