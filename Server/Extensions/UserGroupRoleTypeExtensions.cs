using Data.Model;

namespace Server.Extensions;

public static class UserGroupRoleTypeExtensions
{
    private static readonly UserGroupRoleType[] RoleHierarchy =
    [
        UserGroupRoleType.Member,
        UserGroupRoleType.Admin,
        UserGroupRoleType.Owner
    ];

    public static int GetRoleStrength(this UserGroupRoleType? role)
    {
        if (role is null)
            return -1;

        return Array.IndexOf(RoleHierarchy, (UserGroupRoleType)role);
    }
    
    public static int GetRoleStrength(this UserGroupRoleType role)
    {
        return Array.IndexOf(RoleHierarchy, role);
    }
}