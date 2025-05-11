using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Extensions;
using Server.Model;
using Server.Utils;

namespace Server.Services;

public interface IUserGroupService
{
    /// <summary>
    /// Creates a new user group with an optional name and assigns the creator as the owner of the group.
    /// </summary>
    /// <param name="name">An optional name for the user group. If null, the group will be created without a name.</param>
    /// <returns>The ID of the created group</returns>
    Task<int> Create(string? name = null);

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
}

public class UserGroupService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IServiceUtils utils) : IUserGroupService
{
    public async Task<int> Create(string? name = null)
    {
        var user = await utils.GetUser();

        string? encryptedName = null;
        if (name is not null)
        {
            encryptedName = encryptionService.EncryptAesStringBase64(name);
        }

        var group = new UserGroup
        {
            Name = encryptedName
        };

        await context.UserGroups.AddAsync(group);

        // The creator is the owner
        var groupRole = new UserGroupRole
        {
            UserGroup = group,
            User = user,
            Role = UserGroupRoleType.Owner
        };

        await context.UserGroupRoles.AddAsync(groupRole);
        await context.SaveChangesAsync();

        return group.Id;
    }

    public async Task Update(int groupId, UpdateGroupDto updateGroup)
    {
        var user = await utils.GetUser();
        var role = await GetUserGroupRoleAsync(groupId, user.Id);

        if (role is null)
            throw new ForbiddenException("You don't have a permission to view this group");

        var group = await GetGroupAsync(groupId);
        group.Name = updateGroup.Name is null ? null : encryptionService.EncryptAesStringBase64(updateGroup.Name);
        await context.SaveChangesAsync();
    }

    public async Task<List<GroupDto>> GetUserGroups()
    {
        var user = await utils.GetUser();

        var groups = from groupRole in context.UserGroupRoles
            join userGroup in context.UserGroups on groupRole.UserGroupId equals userGroup.Id
            where groupRole.UserId == user.Id
            select new GroupDto(userGroup.Id, encryptionService.DecryptAesStringBase64(userGroup.Name), groupRole.Role);

        return await groups.ToListAsync();
    }

    public async Task<List<GroupUserDto>> GetGroupMembers(int groupId)
    {
        var currentUser = await utils.GetUser();
        var role = await GetUserGroupRoleAsync(groupId, currentUser.Id);

        if (role is null)
            throw new ForbiddenException("You don't have a permission to view this group");

        var groupUsers = from groupRole in context.UserGroupRoles
            join user in context.Users on groupRole.UserId equals user.Id
            where groupRole.UserGroupId == groupId
            select new GroupUserDto(user.Id, user.Username, user.Name, groupRole.Role);

        return await groupUsers.ToListAsync();
    }

    public async Task AssignRole(int groupId, int userId, UserGroupRoleType role)
    {
        if (role is UserGroupRoleType.Owner)
        {
            throw new InvalidOperationException("Cannot change role to Owner");
        }

        var currentUser = await utils.GetUser();
        var currentUserRole = (await GetUserGroupRoleAsync(groupId, currentUser.Id))?.Role;

        if (currentUserRole is not (UserGroupRoleType.Owner or UserGroupRoleType.Admin))
            throw new ForbiddenException("You don't have the permission to change user roles");

        var user = await context.Users.FindAsync(userId);

        if (user is null)
        {
            throw new UserNotFoundException($"User with {userId} not found");
        }

        var group = await GetGroupAsync(groupId);
        var userRole = await GetUserGroupRoleAsync(groupId, userId);
        var userRoleType = userRole?.Role;

        if (userRoleType is UserGroupRoleType.Owner)
        {
            throw new UserIsOwnerException("Cannot change role of the owner");
        }

        var currentUserRoleStrength = currentUserRole.GetRoleStrength();
        var userRoleStrength = userRoleType.GetRoleStrength();

        if (currentUserRoleStrength <= userRoleStrength && currentUser.Id != userId)
        {
            throw new ForbiddenException("You don't have the permission to change roles of more privileged users");
        }

        if (userRole is null)
        {
            await context.UserGroupRoles.AddAsync(new UserGroupRole
            {
                User = user,
                UserGroup = group,
                Role = role
            });
        }
        else
        {
            userRole.Role = role;
        }

        await context.SaveChangesAsync();
    }

    public async Task RemoveUser(int groupId, int userId)
    {
        var toRemoveRole = await GetUserGroupRoleAsync(groupId, userId);
        if (toRemoveRole is null)
        {
            throw new InvalidOperationException("User is not part of the group");
        }

        // Prevent removing if the user is the owner
        if (toRemoveRole.Role == UserGroupRoleType.Owner)
        {
            throw new UserIsOwnerException("Owner cannot be removed from the group");
        }

        var currentUser = await utils.GetUser();

        // Users can always remove themselves from the group, except in the case of owners
        if (currentUser.Id == userId)
        {
            context.UserGroupRoles.Remove(toRemoveRole);
            await context.SaveChangesAsync();
            return;
        }

        var currentUserRole = await GetUserGroupRoleAsync(groupId, currentUser.Id);

        var toRemoveUserRoleType = (UserGroupRoleType?)toRemoveRole.Role;
        var currentUserRoleType = currentUserRole?.Role;

        var toRemoveRoleStrength = toRemoveUserRoleType.GetRoleStrength();
        var currentUserRoleStrength = currentUserRoleType.GetRoleStrength();

        if (currentUserRoleStrength <= toRemoveRoleStrength)
        {
            throw new ForbiddenException("You do not have permission to remove users from the group.");
        }

        context.UserGroupRoles.Remove(toRemoveRole);
        await context.SaveChangesAsync();
    }

    public async Task<string> GenerateGroupAccessCode(int groupId, DateTime expiryTime)
    {
        var currentUser = await utils.GetUser();
        var currentUserRole = await GetUserGroupRoleAsync(groupId, currentUser.Id);
        if (currentUserRole?.Role is not (UserGroupRoleType.Owner or UserGroupRoleType.Admin))
        {
            throw new ForbiddenException("You cannot invite other users to the group");
        }

        var group = await GetGroupAsync(groupId);

        var groupAccessCode = TokenService.GenerateGroupAccessCode(currentUser, group, expiryTime);

        await context.GroupAccessCodes.AddAsync(groupAccessCode);
        await context.SaveChangesAsync();

        return groupAccessCode.Value;
    }

    public async Task AddCurrentUserToGroup(int groupId, string accessCode)
    {
        var currentUser = await utils.GetUser();
        var existingRole = await GetUserGroupRoleAsync(groupId, currentUser.Id);

        if (existingRole is not null)
        {
            throw new InvalidOperationException("User is already a member of the group");
        }

        var dbAccessCode = await context.GroupAccessCodes
            .Where(code => code.GroupId == groupId && code.Value == accessCode)
            .FirstOrDefaultAsync();

        if (dbAccessCode is null)
        {
            throw new AccessCodeInvalidException("Access code is invalid");
        }

        if (dbAccessCode.IsExpired)
        {
            throw new AccessCodeInvalidException("Access code has expired");
        }

        var group = await GetGroupAsync(groupId);

        await context.UserGroupRoles.AddAsync(new UserGroupRole
        {
            User = currentUser,
            UserGroup = group,
            Role = UserGroupRoleType.Member
        });

        await context.SaveChangesAsync();
    }

    public async Task<UserGroup> GetGroupAsync(int id)
    {
        var group = await context.UserGroups.FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
        {
            throw new UserGroupNotFoundException($"Group with id {id} not found.");
        }

        return group;
    }

    public async Task<UserGroupRole?> GetUserGroupRoleAsync(int groupId, int userId)
    {
        var role = await context.UserGroupRoles
            .Where(group => group.UserGroupId == groupId && group.UserId == userId)
            .FirstOrDefaultAsync();

        return role;
    }
}