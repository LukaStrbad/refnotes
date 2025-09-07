using Api.Exceptions;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Files;

public sealed class FileShareService : IFileShareService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<FileShareService> _logger;
    private readonly IEncryptionService _encryptionService;

    public FileShareService(RefNotesContext context, ILogger<FileShareService> logger,
        IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
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

    public async Task<SharedFile?> GetUserSharedFile(string path, User user)
    {
        var (dirPath, filename) = FileUtils.SplitDirAndFile(path);
        var dirPathHash = _encryptionService.HashString(dirPath);
        var encryptedDir = await _context.Directories
            .Where(dir => dir.OwnerId == user.Id)
            .FirstOrDefaultAsync(dir => dir.PathHash == dirPathHash);
        if (encryptedDir is null)
            return null;

        var filenameHash = _encryptionService.HashString(filename);

        var sharedFiles = from sharedFile in _context.SharedFiles
            join encryptedFile in _context.Files on sharedFile.SharedEncryptedFileId equals encryptedFile.Id
            where encryptedFile.NameHash == filenameHash
            select sharedFile;
        return await sharedFiles.FirstOrDefaultAsync();
    }

    public async Task<EncryptedFile> GetEncryptedFileFromSharedFile(SharedFile sharedFile)
    {
        await _context.Entry(sharedFile).Reference(sf => sf.SharedEncryptedFile).LoadAsync();
        return sharedFile.SharedEncryptedFile ?? throw new Exception("Shared file not found.");
    }

    public async Task Delete(SharedFile sharedFile)
    {
        _context.Entry(sharedFile).State = EntityState.Deleted;
        await _context.SaveChangesAsync();
    }
}
