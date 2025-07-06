using Api.Extensions;
using Api.Model;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class FavoriteService : IFavoriteService
{
    private readonly RefNotesContext _context;
    private readonly IUserService _userService;
    private readonly IFileService _fileService;
    private readonly IEncryptionService _encryptionService;

    public FavoriteService(RefNotesContext context, IUserService userService, IFileService fileService,
        IEncryptionService encryptionService)
    {
        _context = context;
        _userService = userService;
        _fileService = fileService;
        _encryptionService = encryptionService;
    }

    public async Task FavoriteFile(EncryptedFile file)
    {
        var user = await _userService.GetUser();
        var existingFavorite = await _context.FileFavorites.Where(f => f.User == user)
            .FirstOrDefaultAsync(f => f.EncryptedFile == file);
        if (existingFavorite is not null)
            return;

        var newFavorite = new FileFavorite
        {
            User = user,
            EncryptedFile = file
        };
        await _context.AddAsync(newFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task UnfavoriteFile(EncryptedFile file)
    {
        var user = await _userService.GetUser();
        var existingFavorite = await _context.FileFavorites.Where(f => f.User == user)
            .FirstOrDefaultAsync(f => f.EncryptedFile == file);
        if (existingFavorite is null)
            return;

        _context.FileFavorites.Remove(existingFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FileFavoriteDetails>> GetFavoriteFiles()
    {
        var user = await _userService.GetUser();
        var favorites = await _context.FileFavorites.Where(f => f.User == user)
            .ToListAsync();

        var favoriteDetailsList = new List<FileFavoriteDetails>();
        foreach (var favorite in favorites)
        {
            var fileInfo = await _fileService.GetFileInfoAsync(favorite.EncryptedFileId);
            if (fileInfo is not null)
                favoriteDetailsList.Add(new FileFavoriteDetails(fileInfo, favorite.FavoriteDate));
        }

        return favoriteDetailsList;
    }

    public async Task FavoriteDirectory(EncryptedDirectory directory)
    {
        var user = await _userService.GetUser();
        var existingFavorite = await _context.DirectoryFavorites.Where(f => f.User == user)
            .FirstOrDefaultAsync(f => f.EncryptedDirectory == directory);
        if (existingFavorite is not null)
            return;

        var newFavorite = new DirectoryFavorite
        {
            User = user,
            EncryptedDirectory = directory
        };
        await _context.AddAsync(newFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task UnfavoriteDirectory(EncryptedDirectory directory)
    {
        var user = await _userService.GetUser();
        var existingFavorite = await _context.DirectoryFavorites.Where(f => f.User == user)
            .FirstOrDefaultAsync(f => f.EncryptedDirectory == directory);
        if (existingFavorite is null)
            return;

        _context.DirectoryFavorites.Remove(existingFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DirectoryFavoriteDetails>> GetFavoriteDirectories()
    {
        var user = await _userService.GetUser();
        var favorites = _context.DirectoryFavorites
            .Include(f => f.EncryptedDirectory)
            .Where(f => f.User == user);

        var favoriteDetailsList = new List<DirectoryFavoriteDetails>();
        foreach (var favorite in favorites)
        {
            var path = favorite.EncryptedDirectory?.DecryptedPath(_encryptionService);
            if (path is not null)
                favoriteDetailsList.Add(new DirectoryFavoriteDetails(path, favorite.FavoriteDate));
        }

        return favoriteDetailsList;
    }
}
