using System.Text;
using Server;
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
    
    protected AppConfiguration AppConfig => new() { DataDir = TestFolder };

    public BaseTests()
    {
        TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(TestFolder);
    }


    public void Dispose()
    {
        if (Directory.Exists(TestFolder))
        {
            Directory.Delete(TestFolder, true);
        }
    }
}