using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ServiceTests;

public class BrowserServiceTests : BaseTests
{
    private readonly RefNotesContext _context;
    private readonly BrowserService _browserService;
    private readonly User _testUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly EncryptionService _encryptionService;

    public BrowserServiceTests()
    {
        _encryptionService = new EncryptionService(AesKey, AesIv);
        _context = CreateDb();
        (_testUser, _claimsPrincipal) = CreateUser(_context, "test");
        _browserService = new BrowserService(_context, _encryptionService, AppConfig);
    }
    
    [Fact]
    public async Task AddRootDirectory_AddsDirectory()
    {
        await _browserService.AddDirectory(_claimsPrincipal, "/");
        
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == _encryptionService.EncryptAesStringBase64("/"));
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectory_ThrowsExceptionIfUserNotFound()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "nonexistent")
        ]));
        
        await Assert.ThrowsAsync<UserNotFoundException>(() => _browserService.AddDirectory(claimsPrincipal, "/"));
    }
    
    [Fact]
    public async Task AddDirectoryToRoot_AddsDirectory()
    {
        const string path = "/test";
        
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        var encryptedPath = _encryptionService.EncryptAesStringBase64(path);
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath);
        Assert.NotNull(directory);
    }
    
    [Fact]
    public async Task AddDirectoryToSubdirectory_AddsDirectory()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string subPath = "/test/sub";
        await _browserService.AddDirectory(_claimsPrincipal, subPath);
        
        var dirCount = await _context.Directories.CountAsync();
        // Root directory + test directory + subdirectory
        Assert.Equal(3, dirCount);
        
        var encryptedPath = _encryptionService.EncryptAesStringBase64(subPath);
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath);
        Assert.NotNull(directory);
    }
    
    [Fact]
    public async Task AddDirectory_ThrowsIfDirectoryAlreadyExists()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        await Assert.ThrowsAsync<ArgumentException>(() => _browserService.AddDirectory(_claimsPrincipal, path));
    }
    
    [Fact]
    public async Task AddFile_AddsFile()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string fileName = "testfile.txt";
        await _browserService.AddFile(_claimsPrincipal, path, fileName);
        
        var encryptedPath = _encryptionService.EncryptAesStringBase64(path);
        var encryptedFileName = _encryptionService.EncryptAesStringBase64(fileName);
        
        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == encryptedPath);
        
        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(encryptedFileName, directory.Files[0].Name);
    }
    
    [Fact]
    public async Task AddFile_ThrowsIfFileAlreadyExists()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string fileName = "testfile.txt";
        await _browserService.AddFile(_claimsPrincipal, path, fileName);
        
        await Assert.ThrowsAsync<ArgumentException>(() => _browserService.AddFile(_claimsPrincipal, path, fileName));
    }
    
    [Fact]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist()
    {
        const string path = "/test";
        const string fileName = "testfile.txt";
        
        await Assert.ThrowsAsync<ArgumentException>(() => _browserService.AddFile(_claimsPrincipal, path, fileName));
    }
    
    [Fact]
    public async Task List_ReturnsRootDirectory()
    {
        var responseDirectory = await _browserService.List(_claimsPrincipal);

        Assert.NotNull(responseDirectory);
        Assert.Equal("/", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }
    
    [Fact]
    public async Task List_ReturnsDirectory()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        var rootDirectory = await _browserService.List(_claimsPrincipal);
        Assert.NotNull(rootDirectory);
        Assert.Single(rootDirectory.Directories);
        Assert.Empty(rootDirectory.Files);
        Assert.Equal("test", rootDirectory.Directories[0]);
        
        var responseDirectory = await _browserService.List(_claimsPrincipal, path);
        
        Assert.NotNull(responseDirectory);
        Assert.Equal("test", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }
    
    [Fact]
    public async Task List_ReturnsFile()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string fileName = "testfile.txt";
        await _browserService.AddFile(_claimsPrincipal, path, fileName);
        
        var responseDirectory = await _browserService.List(_claimsPrincipal, path);
        
        Assert.NotNull(responseDirectory);
        Assert.Equal("test", responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files[0]);
    }
    
    [Fact]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        const string path = "/test";
        
        var responseDirectory = await _browserService.List(_claimsPrincipal, path);
        
        Assert.Null(responseDirectory);
    }
    
    [Fact]
    public async Task GetFilesystemFilePath_ReturnsFilePath()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string fileName = "testfile.txt";
        var addedFilePath = await _browserService.AddFile(_claimsPrincipal, path, fileName);
        
        var filePath = await _browserService.GetFilesystemFilePath(_claimsPrincipal, path, fileName);
        
        Assert.NotNull(filePath);
        Assert.Equal(addedFilePath, filePath);
    }
    
    [Fact]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        var filePath = await _browserService.GetFilesystemFilePath(_claimsPrincipal, path, "nonexistent.txt");
        
        Assert.Null(filePath);
    }
    
    [Fact]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist()
    {
        const string path = "/test";
        
        await Assert.ThrowsAsync<ArgumentException>(() => _browserService.GetFilesystemFilePath(_claimsPrincipal, path, "testfile.txt"));
    }
    
}