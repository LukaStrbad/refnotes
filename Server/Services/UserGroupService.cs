using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Extensions;
using Server.Model;
using Server.Utils;

namespace Server.Services;

public class UserGroupService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IServiceUtils utils)
{
    public async Task Create(string? name = null)
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
    }

    public async Task Update(UpdateGroupDto updateGroup)
    {
        var user = await utils.GetUser();
        var role = await GetUserGroupRoleAsync(updateGroup.GroupId, user.Id);

        if (role is null)
            throw new ForbiddenException("You don't have a permission to view this group");

        var group = await GetGroupAsync(updateGroup.GroupId);
        group.Name = updateGroup.Name is null ? null : encryptionService.EncryptAesStringBase64(updateGroup.Name);
        await context.SaveChangesAsync();
    }

    public async Task<List<GroupDto>> GetUserGroups()
    {
        var user = await utils.GetUser();

        var groups = from groupRole in context.UserGroupRoles
            join userGroup in context.UserGroups on groupRole.UserGroupId equals userGroup.Id
            where groupRole.UserId == user.Id
            select new GroupDto(userGroup.Id, userGroup.Name, groupRole.Role);

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
            throw new ForbiddenException("You can't have the permission to change roles of more privileged users");
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
            throw new InvalidOperationException("User was not part of the group");
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

    public async Task AddCurrentUserToGroup(int groupId, string accessCode)
    {
        throw new NotImplementedException();
    }

    private async Task<UserGroup> GetGroupAsync(int id)
    {
        var group = await context.UserGroups.FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
        {
            throw new UserGroupNotFoundException($"Group with id {id} not found.");
        }

        return group;
    }

    private async Task<UserGroupRole?> GetUserGroupRoleAsync(int groupId, int userId)
    {
        var role = await context.UserGroupRoles
            .Where(group => group.UserGroupId == groupId && group.UserId == userId)
            .FirstOrDefaultAsync();

        return role;
    }
}