using System.Security.Claims;
using Server;
using Server.Db;
using Server.Db.Model;

namespace ServerTests;

public class BaseTests : IDisposable
{
    private static readonly Random Rnd = new();

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

    protected AppConfiguration AppConfig => new()
        { DataDir = TestFolder, JwtPrivateKey = "test_jwt_private_key_123456789234234247" };

    protected BaseTests()
    {
        TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestFolder);
    }

    protected static (User, ClaimsPrincipal) CreateUser(RefNotesContext context, string username, params string[] roles)
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
        ], "fake auth"));

        return (newUser, claimsPrincipal);
    }

    protected static string RandomString(int length)
    {
        lock (Rnd)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Rnd.Next(s.Length)]).ToArray());
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }

        GC.SuppressFinalize(this);
    }
}