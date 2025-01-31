using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.Db;
using Server.Db.Model;

namespace ServerTests;

public class BaseTests : IDisposable
{
    private RefNotesContext? _context;
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

    public BaseTests()
    {
        TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestFolder);
    }

    protected RefNotesContext CreateDb()
    {
        var className = GetType().Name;
        // Create test db with current class name
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                               $"Server=127.0.0.1;Database=refnotes_test_{className};Uid=root;Pwd=root;";

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        // Create test db
        var dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
            .UseMySql(connectionString, serverVersion).Options;
        _context = new RefNotesContext(dbOptions);
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        return _context;
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
        ]));

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
        // Delete test db
        _context?.Database.EnsureDeleted();

        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }

        GC.SuppressFinalize(this);
    }
}