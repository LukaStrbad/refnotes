using Data.Model;

namespace Api.Model;

public record GroupDto(int Id, string? Name, UserGroupRoleType Role);