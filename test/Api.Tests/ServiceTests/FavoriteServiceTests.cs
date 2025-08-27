using Api.Model;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class FavoriteServiceTests
{
    private readonly FavoriteService _service;
    private readonly FakerResolver _fakerResolver;
    private readonly RefNotesContext _context;
    private readonly IFileService _fileService;

    private readonly User _defaultUser;
    private readonly UserGroup _defaultGroup;

    public FavoriteServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<FavoriteService>().WithDb(dbFixture).WithFakeEncryption().WithFakers()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<FavoriteService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _fileService = serviceProvider.GetRequiredService<IFileService>();

        _defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
        _fakerResolver.Get<UserGroupRole>().ForUser(_defaultUser).ForGroup(_defaultGroup).Generate();
        serviceProvider.GetRequiredService<IUserService>().GetCurrentUser().Returns(_defaultUser);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task FavoriteFile_FavoritesFile(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        await _service.FavoriteFile(file);

        var favorite = await _context.FileFavorites.FirstOrDefaultAsync(f => f.EncryptedFile == file,
            TestContext.Current.CancellationToken);
        Assert.NotNull(favorite);
        Assert.Equal(_defaultUser.Id, favorite.UserId);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task FavoriteFile_DoesntFavoriteFileIfAlreadyFavorite(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        await _service.FavoriteFile(file);
        await _service.FavoriteFile(file);

        var favorites = await _context.FileFavorites.Where(f => f.EncryptedFile == file)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(favorites);
        Assert.Equal(_defaultUser.Id, favorites[0].UserId);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UnfavoriteFile_UnfavoritesFile(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fakerResolver.Get<FileFavorite>().ForUser(_defaultUser).ForFile(file).Generate();

        await _service.UnfavoriteFile(file);

        var favoriteInDb = await _context.FileFavorites.FirstOrDefaultAsync(f => f.EncryptedFile == file,
            TestContext.Current.CancellationToken);
        Assert.Null(favoriteInDb);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFavoriteFiles_ReturnsFavoriteFiles(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var files = _fakerResolver.Get<EncryptedFile>().ForDir(dir);
        var favorites = _fakerResolver.Get<FileFavorite>().ForUser(_defaultUser, files).Generate(5);
        var fileInfos = favorites.Select(f =>
        {
            var encryptedFile = f.EncryptedFile!;
            return new FileDto(encryptedFile.Name, $"/{encryptedFile.Name}", [], 1024, DateTime.UtcNow,
                DateTime.UtcNow);
        }).ToArray();
        _fileService.GetFileInfoAsync(Arg.Any<int>()).Returns(fileInfos[0], fileInfos[1..]);

        var favoriteFiles = await _service.GetFavoriteFiles();

        foreach (var name in favorites.Select(fav => fav.EncryptedFile!.Name))
        {
            Assert.Contains(favoriteFiles, f => f.FileInfo.Name == name);
        }
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task FavoriteDirectory_FavoritesDirectory(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();

        await _service.FavoriteDirectory(dir);

        var favorite = await _context.DirectoryFavorites.FirstOrDefaultAsync(d => d.EncryptedDirectory == dir,
            TestContext.Current.CancellationToken);
        Assert.NotNull(favorite);
        Assert.Equal(_defaultUser.Id, favorite.UserId);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task FavoriteDirectory_DoesntFavoriteDirectoryIfAlreadyFavorite(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();

        await _service.FavoriteDirectory(dir);
        await _service.FavoriteDirectory(dir);

        var favorites = await _context.DirectoryFavorites.Where(d => d.EncryptedDirectory == dir)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(favorites);
        Assert.Equal(_defaultUser.Id, favorites[0].UserId);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UnfavoriteDirectory_UnfavoritesDirectory(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fakerResolver.Get<DirectoryFavorite>().ForDirectory(dir).ForUser(_defaultUser).Generate();

        await _service.UnfavoriteDirectory(dir);

        var favoriteInDb = await _context.DirectoryFavorites.FirstOrDefaultAsync(d => d.EncryptedDirectory == dir,
            TestContext.Current.CancellationToken);
        Assert.Null(favoriteInDb);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFavoriteDirectories_ReturnsFavoriteDirectories(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dirWithOwnerFaker = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group);
        var favorites = _fakerResolver.Get<DirectoryFavorite>().ForUser(_defaultUser).ForDirectory(dirWithOwnerFaker)
            .Generate(5);

        var result = await _service.GetFavoriteDirectories();

        foreach (var path in favorites.Select(fav => fav.EncryptedDirectory!.Path))
        {
            Assert.Contains(result, d => d.Path == path);
        }
    }
}
