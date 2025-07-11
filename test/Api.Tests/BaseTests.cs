﻿using System.Text;

namespace Api.Tests;

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

    protected static Stream StreamFromString(string s)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
        return stream;
    }
}
