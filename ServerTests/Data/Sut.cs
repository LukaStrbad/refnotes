

using Data.Db;
using Data.Db.Model;

namespace ServerTests.Data;

public sealed class Sut<T> where T : class
{
    public T Value { get; init; }

    public User DefaultUser
    {
        get
        {
            var firstUser = Users.FirstOrDefault();
            if (firstUser is null)
                throw new InvalidOperationException("No users in the SUT");
            
            return firstUser;
        }
    }
    
    public RefNotesContext Context { get; init; }
    public IServiceProvider ServiceProvider { get; init; }
    
    public IReadOnlyList<User> Users { get; }
    
    public Sut(T value, RefNotesContext context, IServiceProvider serviceProvider, IReadOnlyList<User> users)
    {
        Value = value;
        Context = context;
        ServiceProvider = serviceProvider;
        Users = users;
    }

    public User GetUser(int index)
    {
        return Users[index];
    }
}