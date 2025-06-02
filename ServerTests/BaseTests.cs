using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server;
using Server.Db.Model;
using Server.Services;
using ServerTests.Mocks;

namespace ServerTests;

public class BaseTests : IDisposable
{
    private static readonly Random Rnd = new();

    protected string TestFolder { get; }

    protected BaseTests()
    {
        TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestFolder);
    }

    protected static string RandomString(int length)
    {
        lock (Rnd)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[Rnd.Next(s.Length)]).ToArray())
                .ToLowerInvariant();
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