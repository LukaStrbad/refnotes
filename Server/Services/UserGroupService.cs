using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Extensions;
using Server.Model;

namespace Server.Services;

public class UserGroupService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IUserService userService) : IUserGroupService
{
    public async Task<GroupDto> Create(User user, string? name = null)
    {
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

        return new GroupDto(group.Id, name, groupRole.Role);
    }
    
    public async Task<GroupDto> Create(string? name = null)
    {
        var user = await userService.GetUser();
        return await Create(user, name);        
    }

    public async Task Update(int groupId, UpdateGroupDto updateGroup)
    {
        var group = await GetGroupAsync(groupId);
        group.Name = updateGroup.Name is null ? null : encryptionService.EncryptAesStringBase64(updateGroup.Name);
        await context.SaveChangesAsync();
    }

    public async Task<List<GroupDto>> GetUserGroups()
    {
        var user = await userService.GetUser();

        var groups = from groupRole in context.UserGroupRoles
            join userGroup in context.UserGroups on groupRole.UserGroupId equals userGroup.Id
            where groupRole.UserId == user.Id
            select new GroupDto(userGroup.Id, encryptionService.DecryptAesStringBase64(userGroup.Name ?? ""), groupRole.Role);

        return await groups.ToListAsync();
    }

    public async Task<List<GroupUserDto>> GetGroupMembers(int groupId)
    {
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

        var currentUser = await userService.GetUser();

        // Users can always remove themselves from the group, except in the case of owners
        if (currentUser.Id == userId)
        {
            context.UserGroupRoles.Remove(toRemoveRole);
            await context.SaveChangesAsync();
            return;
        }

        context.UserGroupRoles.Remove(toRemoveRole);
        await context.SaveChangesAsync();
    }

    public async Task<string> GenerateGroupAccessCode(int groupId, DateTime expiryTime)
    {
        var currentUser = await userService.GetUser();

        var group = await GetGroupAsync(groupId);
        var groupAccessCode = TokenService.GenerateGroupAccessCode(currentUser, group, expiryTime);

        await context.GroupAccessCodes.AddAsync(groupAccessCode);
        await context.SaveChangesAsync();

        return groupAccessCode.Value;
    }

    public async Task AddCurrentUserToGroup(int groupId, string accessCode)
    {
        var currentUser = await userService.GetUser();
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
    
    public async Task<UserGroupRoleType?> GetGroupRoleTypeAsync(int groupId, int userId)
    {
        var role = await GetUserGroupRoleAsync(groupId, userId);
        return role?.Role;
    }
}