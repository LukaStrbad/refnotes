using Data.Model;

namespace Api.Model;

public record GroupUserDto(int Id, string Username, string Name, UserGroupRoleType Role);