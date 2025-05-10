using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server.Controllers;
using Server.Db.Model;
using Server.Model;
using Server.Services;
using Server.Exceptions;

namespace ServerTests.ControllerTests;

public class UserGroupControllerTests
{
    private readonly IUserGroupService _service;
    private readonly UserGroupController _controller;

    public UserGroupControllerTests()
    {
        _service = Substitute.For<IUserGroupService>();
        _controller = new UserGroupController(_service);
    }

    [Fact]
    public async Task Create_ReturnsOk_WithId()
    {
        _service.Create(Arg.Any<string>()).Returns(123);

        var result = await _controller.Create("TestGroup");
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(123, okResult.Value);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var dto = new UpdateGroupDto("New name");
        const int groupId = 10;

        var result = await _controller.Update(groupId, dto);
        var okResult = Assert.IsType<OkResult>(result);

        await _service.Received(1).Update(groupId, dto);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetUserGroups_ReturnsOk_WithGroups()
    {
        var groups = new List<GroupDto> { new(1, "Test Group", UserGroupRoleType.Member) };
        _service.GetUserGroups().Returns(groups);

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
        _service.GetGroupMembers(5).Returns(members);

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

        var result = await _controller.AssignRole(1, dto);
        var okResult = Assert.IsType<OkResult>(result);

        await _service.Received(1).AssignRole(1, dto.UserId, dto.Role);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AssignRole_ReturnsBadRequest_ForInvalidOperationException()
    {
        var dto = new AssignRoleDto(2, UserGroupRoleType.Member);
        _service.AssignRole(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UserGroupRoleType>())
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await _controller.AssignRole(5, dto);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_ForUserNotFoundException()
    {
        var dto = new AssignRoleDto(99, UserGroupRoleType.Member);
        _service.AssignRole(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UserGroupRoleType>())
            .ThrowsAsync(new UserNotFoundException("not found"));

        var result = await _controller.AssignRole(8, dto);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.NotNull(notFoundResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("not found", notFoundResult.Value);
    }

    [Fact]
    public async Task RemoveUser_ReturnsOk()
    {
        var result = await _controller.RemoveUser(2, 3);
        var okResult = Assert.IsType<OkResult>(result);

        await _service.Received(1).RemoveUser(2, 3);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ReturnsBadRequest_ForInvalidOperationException()
    {
        _service.RemoveUser(Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(new InvalidOperationException("fail"));

        var result = await _controller.RemoveUser(1, 1);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
        Assert.Equal("fail", badResult.Value);
    }

    [Fact]
    public async Task GenerateAccessCode_ReturnsOk()
    {
        _service.GenerateGroupAccessCode(1, Arg.Any<DateTime>()).Returns("CODE123");

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

        await _service.Received(1).AddCurrentUserToGroup(5, "code");
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AddCurrentUserWithCode_ReturnsBadRequest_ForInvalidOperationException()
    {
        _service.AddCurrentUserToGroup(Arg.Any<int>(), Arg.Any<string>())
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
        _service.AddCurrentUserToGroup(Arg.Any<int>(), Arg.Any<string>())
            .ThrowsAsync(new AccessCodeInvalidException("forbidden"));

        var result = await _controller.AddCurrentUserWithCode(7, "bad");
        var forbidResult = Assert.IsType<ForbidResult>(result);

        Assert.NotNull(forbidResult);
    }
}