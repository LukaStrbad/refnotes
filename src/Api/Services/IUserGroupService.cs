using Api.Exceptions;
using Api.Model;
using Data.Model;

namespace Api.Services;

public interface IUserGroupService
{
    /// <summary>
    /// Creates a new user group with an optional name and assigns the creator as the owner of the group.
    /// </summary>
    /// <param name="name">An optional name for the user group. If null, the group will be created without a name.</param>
    /// <returns>The ID of the created group</returns>
    Task<GroupDto> Create(string? name = null);

    /// <summary>
    /// Updates a user group with new information.
    /// </summary>
    /// <param name="updateGroup">An instance of <see cref="UpdateGroupDto"/> containing the group ID and updated name of the group.</param>
    /// <exception cref="ForbiddenException">Thrown when the user does not have sufficient permissions to update the group.</exception>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Update(int groupId, UpdateGroupDto updateGroup);

    /// <summary>
    /// Retrieves a list of user groups associated with the currently authenticated user, including their role in each group.
    /// </summary>
    /// <returns>A list of GroupDto objects containing information about each user group and the user's role within it.</returns>
    Task<List<GroupDto>> GetUserGroups();

    /// <summary>
    /// Retrieves a list of members belonging to the specified group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group whose members are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="GroupUserDto"/> representing the members of the group.</returns>
    Task<List<GroupUserDto>> GetGroupMembers(int groupId);

    /// <summary>
    /// Assigns a role to a user in a specific group, ensuring that the current user has the required permissions
    /// and that the role assignment adheres to business rules such as preventing assignment of privileged roles
    /// or changes made by unauthorized users.
    /// </summary>
    /// <param name="groupId">The ID of the group in which the role assignment is taking place.</param>
    /// <param name="userId">The ID of the user to whom the role will be assigned.</param>
    /// <param name="role">The role to be assigned to the user.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to assign the Owner role, which is not allowed.
    /// </exception>
    /// <exception cref="ForbiddenException">
    /// Thrown when the current user does not have sufficient permissions to assign roles
    /// or attempts to modify roles of more privileged users.
    /// </exception>
    /// <exception cref="UserNotFoundException">
    /// Thrown when the user with the specified ID does not exist.
    /// </exception>
    /// <exception cref="UserIsOwnerException">
    /// Thrown when an attempt is made to change the role of the owner of the group.
    /// </exception>
    /// <returns>A task that represents the asynchronous operation of assigning the role.</returns>
    Task AssignRole(int groupId, int userId, UserGroupRoleType role);

    /// <summary>
    /// Removes a user from a specified group.
    /// Ensures that only authorized users can remove others and
    /// enforces the rule that group owners cannot be removed.
    /// </summary>
    /// <param name="groupId">The identifier of the group from which the user should be removed.</param>
    /// <param name="userId">The identifier of the user to be removed from the group.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified user is not part of the group.</exception>
    /// <exception cref="UserIsOwnerException">Thrown if an attempt is made to remove the group owner.</exception>
    /// <exception cref="ForbiddenException">
    /// Thrown if the current user does not have sufficient permissions to remove the specified user.
    /// </exception>
    Task RemoveUser(int groupId, int userId);

    /// <summary>
    /// Generates an access code for a specific user group, allowing other users to join the group.
    /// The access code will have an expiration time and is restricted by the current user's permissions in the group.
    /// </summary>
    /// <param name="groupId">The ID of the group for which the access code is being generated.</param>
    /// <param name="expiryTime">The expiration time until which the access code remains valid.</param>
    /// <returns>A string representing the generated access code for the group.</returns>
    /// <exception cref="ForbiddenException">
    /// Thrown when the current user does not have sufficient permissions to generate an access code for the group.
    /// </exception>
    /// <exception cref="ExpiryTimeTooLongException">
    /// Thrown when the specified expiry time exceeds the maximum allowed limit.
    /// </exception>
    Task<string> GenerateGroupAccessCode(int groupId, DateTime expiryTime);

    /// <summary>
    /// Adds the current user to a specified group using an access code.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="accessCode">The access code required to join the group.</param>
    /// <returns>
    /// A task that represents the asynchronous operation of adding the current user to the group.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user is already a member of the group.
    /// </exception>
    /// <exception cref="AccessCodeInvalidException">
    /// Thrown when the access code is invalid or has expired.
    /// </exception>
    Task AddCurrentUserToGroup(int groupId, string accessCode);

    public Task<UserGroup> GetGroupAsync(int id);

    public Task<UserGroupRole?> GetUserGroupRoleAsync(int groupId, int userId);
    Task<UserGroupRoleType?> GetGroupRoleTypeAsync(int groupId, int userId);
}