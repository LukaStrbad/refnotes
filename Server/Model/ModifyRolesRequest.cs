namespace Server.Model;

public record ModifyRolesRequest(string Username, List<string> AddRoles, List<string> RemoveRoles);