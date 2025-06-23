using Data.Db.Model;

namespace Server.Model;

public record GroupDto(int Id, string? Name, UserGroupRoleType Role);