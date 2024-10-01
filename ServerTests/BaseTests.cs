using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.Db;
using Server.Model;
using Xunit.Abstractions;

namespace ServerTests;

public class BaseTests : IDisposable
{
    protected string TestFolder { get; }

    protected string TestFile
    {
        get
        {
            var fileName = Path.GetRandomFileName();
            return Path.Combine(TestFolder, fileName);
        }
    }
    
    protected byte[] AesKey { get; } = "1234567890123456"u8.ToArray();
    protected byte[] AesIv { get; } = "1234567890123456"u8.ToArray();
    protected const string DefaultPassword = "password";
    
    protected AppConfiguration AppConfig => new() { DataDir = TestFolder, JwtPrivateKey = "test_jwt_private_key_123456789234234247"};

    public BaseTests()
    {
        TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestFolder);
    }

    protected RefNotesContext CreateDb()
    {
        // Create test db
        var dbOptions = new DbContextOptionsBuilder<RefNotesContext>().UseSqlite("Data Source=test.db").Options;
        var context = new RefNotesContext(dbOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    protected (User, ClaimsPrincipal) CreateUser(RefNotesContext context, string username, params string[] roles)
    {
        // Add test user to db
        var newUser = new User(0, username, username, $"{username}@test.com", DefaultPassword)
        {
            Roles = roles
        };
        context.Users.Add(newUser);
        context.SaveChanges();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, newUser.Username),
            new Claim(ClaimTypes.Email, newUser.Email)
        ]));
        
        return (newUser, claimsPrincipal);
    }


    public void Dispose()
    {
        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }
    }
}