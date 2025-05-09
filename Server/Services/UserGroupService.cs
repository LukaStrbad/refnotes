using Server.Db;
using Server.Db.Model;
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
        throw new NotImplementedException();
    }

    public async Task<List<GroupDto>> GetUserGroups()
    {
        throw new NotImplementedException();
    }
    
    public async Task<List<GroupUserDto>> GetGroupMembers(int groupId)
    {
        throw new NotImplementedException();
    }
    
    public async Task AssignRole(int groupId, int userId, UserGroupRoleType role)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveUser(int groupId, int userId)
    {
        throw new NotImplementedException();
    }

    public async Task AddCurrentUserToGroup(int groupId, string accessCode)
    {
        throw new NotImplementedException();
    }
}