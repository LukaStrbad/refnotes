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
        var existingFavorite = await _context.FileFavorites.Where(f => f.UserId == user.Id)
            .FirstOrDefaultAsync(f => f.EncryptedFileId == file.Id);
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
        var existingFavorite = await _context.FileFavorites.Where(f => f.UserId == user.Id)
            .FirstOrDefaultAsync(f => f.EncryptedFileId == file.Id);
        if (existingFavorite is null)
            return;

        _context.FileFavorites.Remove(existingFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FileFavoriteDetails>> GetFavoriteFiles()
    {
        var user = await _userService.GetUser();
        var favorites = await _context.FileFavorites.Where(f => f.UserId == user.Id)
            .Select(f => new { f.EncryptedFileId, f.FavoriteDate })
            .ToListAsync();

        var favoriteDetailsList = new List<FileFavoriteDetails>();
        foreach (var favorite in favorites)
        {
            var fileInfo = await _fileService.GetFileInfoAsync(favorite.EncryptedFileId);
            var groupId = await _fileService.GetGroupIdFromFileAsync(favorite.EncryptedFileId);
            if (fileInfo is not null)
                favoriteDetailsList.Add(new FileFavoriteDetails(fileInfo, groupId, favorite.FavoriteDate));
        }

        return favoriteDetailsList;
    }

    public async Task FavoriteDirectory(EncryptedDirectory directory)
    {
        var user = await _userService.GetUser();
        var existingFavorite = await _context.DirectoryFavorites.Where(f => f.UserId == user.Id)
            .FirstOrDefaultAsync(f => f.EncryptedDirectoryId == directory.Id);
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
        var existingFavorite = await _context.DirectoryFavorites.Where(f => f.UserId == user.Id)
            .FirstOrDefaultAsync(f => f.EncryptedDirectoryId == directory.Id);
        if (existingFavorite is null)
            return;

        _context.DirectoryFavorites.Remove(existingFavorite);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DirectoryFavoriteDetails>> GetFavoriteDirectories()
    {
        var user = await _userService.GetUser();

        var favoritesQuery = from favorite in _context.DirectoryFavorites
            join directory in _context.Directories on favorite.EncryptedDirectoryId equals directory.Id
            where favorite.UserId == user.Id
            select new { directory.Path, directory.GroupId, favorite.FavoriteDate };

        var favorites = await favoritesQuery.ToListAsync();

        return favorites.Select(favorite =>
        {
            var decryptedPath = _encryptionService.DecryptAesStringBase64(favorite.Path);
            return new DirectoryFavoriteDetails(decryptedPath, favorite.GroupId, favorite.FavoriteDate);
        }).ToList();
    }
}
