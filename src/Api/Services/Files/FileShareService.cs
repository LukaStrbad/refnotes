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
            throw new DirectoryNotFoundException();
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
}
