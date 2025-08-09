using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Services.Redis;
using Api.Tests.Fixtures;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;

namespace Api.Tests.ServiceTests;

using FixtureType = ServiceFixture<FileStorageService>;

[SuppressMessage("Usage", "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public class FileStorageServiceTests : BaseTests, IClassFixture<FixtureType>
{
    private readonly string _fileName = $"{RandomString(32)}.txt";

    private readonly FileStorageService _service;
    private readonly IDatabase _redis;
    private readonly AppSettings _appSettings;
    private readonly IRedisLockProvider _lockProvider;
    private readonly IDistributedSynchronizationHandle _handle = Substitute.For<IDistributedSynchronizationHandle>();

    public FileStorageServiceTests(FixtureType fixture)
    {
        var serviceProvider = fixture.WithRedis().WithFakeEncryption().CreateServiceProvider();
        _service = serviceProvider.GetRequiredService<FileStorageService>();
        _redis = serviceProvider.GetRequiredService<IDatabase>();
        _appSettings = serviceProvider.GetRequiredService<AppSettings>();
        _lockProvider = serviceProvider.GetRequiredService<IRedisLockProvider>();
    }

    [Fact]
    public async Task SaveFile_CreatesEncryptedFile()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        _lockProvider.TryAcquireWriteLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(_handle);
        var filePath = Path.Combine(_appSettings.DataDir, _fileName);
        var fileSizeKey = new RedisKey($"FileSize:{_fileName}");

        await _service.SaveFileAsync(_fileName, inputStream);
        
        Assert.True(File.Exists(filePath));
        var encryptedContent = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
        // We are not testing the encryption algorithm here, so we don't need to decrypt the content
        Assert.Equal("test content", encryptedContent);
        await _redis.Received(1).StringSetAsync(fileSizeKey, inputStream.Length);
    }

    [Fact]
    public async Task SaveFile_ThrowsException_WhenFileNameIsEmpty()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveFileAsync("", inputStream));
    }

    [Fact]
    public async Task GetFile_ReturnsDecryptedStream()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        await _service.SaveFileAsync(_fileName, inputStream);

        await using var stream = _service.GetFile(_fileName);

        using var sr = new StreamReader(stream);
        var content = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test content", content);
    }

    [Fact]
    public async Task GetFile_ThrowsIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() => Task.FromResult(_service.GetFile(fileName)));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        var sizeKey = new RedisKey($"FileSize:{_fileName}");
        await _service.SaveFileAsync(_fileName, inputStream);

        await _service.DeleteFile(_fileName);

        var filePath = Path.Combine(TestFolder, _fileName);
        Assert.False(File.Exists(filePath));
        await _redis.Received(1).KeyDeleteAsync(sizeKey);
    }

    [Fact]
    public async Task DeleteFile_DoesNothingIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await _service.DeleteFile(fileName);
    }

    [Fact]
    public async Task GetFileSize_ReturnsFileSize()
    {
        var fileContent = "test content"u8.ToArray();
        var sizeKey = new RedisKey($"FileSize:{_fileName}");
        var sizeLong = fileContent.LongLength;
        await using var inputStream = new MemoryStream(fileContent);
        await _service.SaveFileAsync(_fileName, inputStream);
        _redis.ClearReceivedCalls();

        var fileSize = await _service.GetFileSize(_fileName);

        Assert.Equal(fileContent.LongLength, fileSize);
        await _redis.Received(1).StringSetAsync(sizeKey, sizeLong);
    }

    [Fact]
    public async Task GetFileSize_ReturnsValueFromCache()
    {
        var sizeKey = new RedisKey($"FileSize:{_fileName}");
        var fileSize = new RedisValue("12345");
        _redis.StringGetAsync(sizeKey).Returns(fileSize);
        // Create the file to ensure the method doesn't throw
        await using var _ = File.Create(Path.Combine(_appSettings.DataDir, _fileName));
        
        var fileSizeLong = await _service.GetFileSize(_fileName);
        
        Assert.Equal(12345L, fileSizeLong);
    }
}
