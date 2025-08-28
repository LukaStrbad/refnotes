using Api.Exceptions;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Files;

public sealed class FileShareService : IFileShareService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<FileShareService> _logger;

    public FileShareService(RefNotesContext context, ILogger<FileShareService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateShareHash(int encryptedFileId)
    {
        var file = await _context.Files.FindAsync(encryptedFileId);
        if (file is null)
        {
            _logger.LogError("File with ID {encryptedFileId} not found.", encryptedFileId);
            throw new FileNotFoundException();
        }

        var hash = Guid.CreateVersion7().ToString();
        var sharedFileHash = new SharedFileHash
        {
            EncryptedFile = file,
            Hash = hash
        };
        await _context.SharedFileHashes.AddAsync(sharedFileHash);
        await _context.SaveChangesAsync();
        return hash;
    }

    public async Task<SharedFile> GenerateSharedFileFromHash(string hash, int attachedDirectoryId)
    {
        var sharedFileHash = await _context.SharedFileHashes
            .Include(sf => sf.EncryptedFile)
            .Where(sf => !sf.IsDeleted)
            .FirstOrDefaultAsync(sf => sf.Hash == hash);
        if (sharedFileHash is null)
        {
            _logger.LogError("Shared file with hash {hash} not found.", hash);
            throw new SharedFileHashNotFound("Shared file not found.");
        }

        var directory = await _context.Directories.FindAsync(attachedDirectoryId);
        if (directory is null)
        {
            _logger.LogError("Directory with ID {directoryId} not found.", attachedDirectoryId);
            throw new DirectoryNotFoundException("Directory not found");
        }

        var sharedFile = new SharedFile
        {
            SharedToDirectory = directory,
            SharedEncryptedFile = sharedFileHash.EncryptedFile
        };
        await _context.SharedFiles.AddAsync(sharedFile);
        sharedFileHash.IsDeleted = true;
        await _context.SaveChangesAsync();
        return sharedFile;
    }

    public async Task<User?> GetOwnerFromHash(string hash)
    {
        var sharedFileHash = await _context.SharedFileHashes
            .Include(sf => sf.EncryptedFile)
            .Where(sf => !sf.IsDeleted)
            .FirstOrDefaultAsync(sf => sf.Hash == hash);
        if (sharedFileHash is null)
            return null;

        var directory = await _context.Directories
            .Include(d => d.Owner)
            .FirstOrDefaultAsync(d => d.Id == sharedFileHash.EncryptedFile.EncryptedDirectoryId);

        return directory?.Owner;
    }

    public async Task<List<SharedFile>> GetSharedFilesForUser(int userId)
    {
        // Get all directories owned by the user
        var userDirectories = await _context.Directories
            .Where(d => d.Owner!.Id == userId)
            .Select(d => d.Id)
            .ToListAsync();

        // Get all shared files in those directories
        var sharedFiles = await _context.SharedFiles
            .Include(sf => sf.SharedEncryptedFile)
            .Include(sf => sf.SharedToDirectory)
            .Where(sf => userDirectories.Contains(sf.SharedToDirectoryId))
            .OrderByDescending(sf => sf.Created)
            .ToListAsync();

        return sharedFiles;
    }
}
