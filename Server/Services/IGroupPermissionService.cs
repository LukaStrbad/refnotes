using Server.Db.Model;

namespace Server.Services;

/// <summary>
/// Checks for file and group permissions.
/// </summary>
public interface IGroupPermissionService
{
    /// <summary>
    /// Checks if the user has access to the specified group.
    /// </summary>
    /// <param name="user">User to check</param>
    /// <param name="groupId">The group ID</param>
    /// <returns>True if the user has access to the group, false otherwise</returns>
    Task<bool> HasGroupAccessAsync(User user, int groupId);
    
    /// <summary>
    /// Checks if the user has access to the specified group with a minimum required role.
    /// </summary>
    /// <param name="user">User to check</param>
    /// <param name="groupId">The group ID</param>
    /// <param name="minRole">The minimum role required for access</param>
    /// <returns>True if the user has access to the group with the required role, false otherwise</returns>
    Task<bool> HasGroupAccessAsync(User user, int groupId, UserGroupRoleType minRole);

    /// <summary>
    /// Check if the user can manage the specified role in the specified group.
    /// </summary>
    /// <param name="user">User to check</param>
    /// <param name="groupId">The group ID</param>
    /// <param name="role">The role to manage</param>
    /// <returns>True if the user can manage the role within the group, false otherwise</returns>
    Task<bool> CanManageRoleAsync(User user, int groupId, UserGroupRoleType role);
    
    /// <summary>
    /// Check if the user can manage the specified user in the specified group.
    /// </summary>
    /// <param name="user">User that tries to manage</param>
    /// <param name="groupId">The group ID</param>
    /// <param name="userId">ID of the other user</param>
    /// <returns>True if managing user can manage the specified user, false otherwise</returns>
    Task<bool> CanManageUserAsync(User user, int groupId, int userId);
}