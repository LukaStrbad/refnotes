using Api.Model;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Api.Tests.Mocks;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Api.Tests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
public class FavoriteServiceTests
{
    [Theory, AutoData]
    public async Task FavoriteFile_FavoritesFile(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        var file = fileFaker.CreateFaker().ForDir(dir).Generate();

        await sut.Value.FavoriteFile(file);

        var favorite = await sut.Context.FileFavorites.FirstOrDefaultAsync(f => f.EncryptedFile == file,
            TestContext.Current.CancellationToken);
        Assert.NotNull(favorite);
        Assert.Equal(sut.DefaultUser.Id, favorite.UserId);
    }

    [Theory, AutoData]
    public async Task FavoriteFile_DoesntFavoriteFileIfAlreadyFavorite(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        var file = fileFaker.CreateFaker().ForDir(dir).Generate();

        await sut.Value.FavoriteFile(file);
        await sut.Value.FavoriteFile(file);

        var favorites = await sut.Context.FileFavorites.Where(f => f.EncryptedFile == file)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(favorites);
        Assert.Equal(sut.DefaultUser.Id, favorites[0].UserId);
    }

    [Theory, AutoData]
    public async Task UnfavoriteFile_UnfavoritesFile(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker,
        FileFavoriteFakerImplementation favoriteFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        var file = fileFaker.CreateFaker().ForDir(dir).Generate();
        favoriteFaker.CreateFaker().ForUser(sut.DefaultUser).ForFile(file).Generate();

        await sut.Value.UnfavoriteFile(file);

        var favoriteInDb = await sut.Context.FileFavorites.FirstOrDefaultAsync(f => f.EncryptedFile == file,
            TestContext.Current.CancellationToken);
        Assert.Null(favoriteInDb);
    }

    [Theory, AutoData]
    public async Task GetFavoriteFiles_ReturnsFavoriteFiles(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker,
        FileFavoriteFakerImplementation favoriteFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        var files = fileFaker.CreateFaker().ForDir(dir);
        var favorites = favoriteFaker.CreateFaker().ForUser(sut.DefaultUser, files).Generate(5);
        var fileInfos = favorites.Select(f =>
        {
            var encryptedFile = f.EncryptedFile!;
            return new FileDto(encryptedFile.Name, $"/{encryptedFile.Name}", [], 1024, DateTime.UtcNow,
                DateTime.UtcNow);
        }).ToArray();
        fileService.GetFileInfoAsync(Arg.Any<int>()).Returns(fileInfos[0], fileInfos[1..]);

        var favoriteFiles = await sut.Value.GetFavoriteFiles();

        foreach (var name in favorites.Select(fav => fav.EncryptedFile!.Name))
        {
            Assert.Contains(favoriteFiles, f => f.FileInfo.Name == name);
        }
    }

    [Theory, AutoData]
    public async Task FavoriteDirectory_FavoritesDirectory(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();

        await sut.Value.FavoriteDirectory(dir);

        var favorite = await sut.Context.DirectoryFavorites.FirstOrDefaultAsync(d => d.EncryptedDirectory == dir,
            TestContext.Current.CancellationToken);
        Assert.NotNull(favorite);
        Assert.Equal(sut.DefaultUser.Id, favorite.UserId);
    }

    [Theory, AutoData]
    public async Task FavoriteDirectory_DoesntFavoriteDirectoryIfAlreadyFavorite(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();

        await sut.Value.FavoriteDirectory(dir);
        await sut.Value.FavoriteDirectory(dir);

        var favorites = await sut.Context.DirectoryFavorites.Where(d => d.EncryptedDirectory == dir)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(favorites);
        Assert.Equal(sut.DefaultUser.Id, favorites[0].UserId);
    }

    [Theory, AutoData]
    public async Task UnfavoriteDirectory_UnfavoritesDirectory(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker,
        DirectoryFavoriteFakerImplementation favoriteFaker)
    {
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        favoriteFaker.CreateFaker().ForDirectory(dir).ForUser(sut.DefaultUser).Generate();

        await sut.Value.UnfavoriteDirectory(dir);

        var favoriteInDb = await sut.Context.DirectoryFavorites.FirstOrDefaultAsync(d => d.EncryptedDirectory == dir,
            TestContext.Current.CancellationToken);
        Assert.Null(favoriteInDb);
    }

    [Theory, AutoData]
    public async Task GetFavoriteDirectories_ReturnsFavoriteDirectories(
        Sut<FavoriteService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedDirectoryFakerImplementation dirFaker,
        DirectoryFavoriteFakerImplementation favoriteFaker)
    {
        var dirWithOwnerFaker = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group);
        var favorites = favoriteFaker.CreateFaker().ForUser(sut.DefaultUser).ForDirectory(dirWithOwnerFaker).Generate(5);

        var result = await sut.Value.GetFavoriteDirectories();

        foreach (var path in favorites.Select(fav => fav.EncryptedDirectory!.Path))
        {
            Assert.Contains(result, d => d.Path == path);
        }
    }
}
