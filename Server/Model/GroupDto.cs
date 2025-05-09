using Server.Db.Model;

namespace Server.Model;

public record GroupDto(int Id, string? Name, UserGroupRoleType Role);