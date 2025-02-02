using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
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
        _browserService = new BrowserService(_context, _encryptionService);
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
    public async Task DeleteDirectory_RemovesDirectory()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        await _browserService.DeleteDirectory(_claimsPrincipal, path);
        
        var directory = await _context.Directories.FirstOrDefaultAsync(d => d.Path == _encryptionService.EncryptAesStringBase64(path));
        Assert.Null(directory);
    }
    
    [Fact]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist()
    {
        const string path = "/test";
        
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _browserService.DeleteDirectory(_claimsPrincipal, path));
    }
    
    [Fact]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty()
    {
        const string path = "/test";
        await _browserService.AddDirectory(_claimsPrincipal, path);
        
        const string subPath = "/test/sub";
        await _browserService.AddDirectory(_claimsPrincipal, subPath);
        
        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() => _browserService.DeleteDirectory(_claimsPrincipal, path));
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
        Assert.Equal("test", rootDirectory.Directories.FirstOrDefault());
        
        var responseDirectory = await _browserService.List(_claimsPrincipal, path);
        
        Assert.NotNull(responseDirectory);
        Assert.Equal("test", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }
    
    [Fact]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        const string path = "/test";
        
        var responseDirectory = await _browserService.List(_claimsPrincipal, path);
        
        Assert.Null(responseDirectory);
    }
    
}