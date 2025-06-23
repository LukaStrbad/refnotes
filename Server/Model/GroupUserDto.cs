using Data.Model;

namespace Server.Model;

public record GroupUserDto(int Id, string Username, string Name, UserGroupRoleType Role);