namespace Data.Model;

public sealed class DirOwner
{
    public User? User { get; }
    public UserGroup? Group { get; }

    public DirOwner(User user)
    {
        User = user;
        Group = null;
    }
    
    public DirOwner(UserGroup group)
    {
        User = null;
        Group = group;
    }
}
