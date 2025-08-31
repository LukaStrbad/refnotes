using Api.Tests.Extensions.Faker;
using Data.Model;
using Bogus;
using Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Fixtures;

public static class FakerCollection
{
    public static IServiceCollection GetFakerCollection(RefNotesContext? context)
    {
        var services = new ServiceCollection();

        services.AddTransient<Faker<User>>(_ => GetUserFaker(context));
        services.AddTransient<Faker<EncryptedDirectory>>(_ => GetDirectoryFaker(context));
        services.AddTransient<Faker<EncryptedFile>>(_ => GetFileFaker(context));
        services.AddTransient<Faker<UserGroup>>(_ => GetUserGroupFaker(context));
        services.AddTransient<Faker<UserGroupRole>>(_ => GetUserGroupRoleFaker(context));
        services.AddTransient<Faker<EmailConfirmToken>>(_ => GetEmailConfirmTokenFaker(context));
        services.AddTransient<Faker<DirectoryFavorite>>(_ => GetDirectoryFavoriteFaker(context));
        services.AddTransient<Faker<FileFavorite>>(_ => GetFileFavoriteFaker(context));
        services.AddTransient<Faker<PasswordResetToken>>(_ => GetPasswordResetTokenFaker(context));
        services.AddTransient<Faker<PublicFile>>(_ => GetPublicFileFaker(context));
        services.AddTransient<Faker<PublicFileImage>>(_ => GetPublicFileImageFaker(context));
        services.AddTransient<Faker<FileTag>>(_ => GetFileTagFaker(context));
        services.AddTransient<Faker<SharedFileHash>>(_ => GetSharedFileHashFaker(context));
        services.AddTransient<Faker<SharedFile>>(_ => GetSharedFileFaker(context));

        return services;
    }

    private static Faker<T> CreateBaseFaker<T>(RefNotesContext? context) where T : class
    {
        return context is null ? new Faker<T>() : new DatabaseFaker<T>(context);
    }

    private static Faker<User> GetUserFaker(RefNotesContext? context) =>
        CreateBaseFaker<User>(context)
            .CustomInstantiator(_ => new User("", "", "", ""))
            .StrictMode(true)
            .RuleFor(u => u.Id, f => 0)
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password())
            .RuleFor(u => u.Roles, _ => [])
            .RuleFor(u => u.EmailConfirmed, _ => true);

    private static Faker<EncryptedDirectory> GetDirectoryFaker(RefNotesContext? context)
    {
        var userFaker = GetUserFaker(context);

        return CreateBaseFaker<EncryptedDirectory>(context)
            .StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Path, f => f.System.DirectoryPath() + Guid.CreateVersion7()) // Ensure unique directory path
            .RuleFor(g => g.PathHash, (_, dir) => dir.Path)
            .RuleFor(g => g.Files, _ => [])
            .RuleFor(g => g.SharedFiles, _ => [])
            .RuleFor(g => g.Directories, _ => [])
            .RuleFor(g => g.Parent, _ => null)
            .RuleFor(g => g.Owner, _ => userFaker.Generate())
            .RuleFor(g => g.OwnerId, (_, dir) => dir.Owner?.Id)
            .RuleFor(g => g.Group, _ => null)
            .RuleFor(d => d.GroupId, (_, dir) => dir.Group?.Id);
    }

    private static Faker<EncryptedFile> GetFileFaker(RefNotesContext? context)
    {
        var dirFaker = GetDirectoryFaker(context);

        return CreateBaseFaker<EncryptedFile>(context)
            .CustomInstantiator(_ => new EncryptedFile("", "", ""))
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.FilesystemName, f => f.System.FileName())
            .RuleFor(f => f.Name, _ => Guid.CreateVersion7().ToString()) // Ensure unique file name
            .RuleFor(f => f.NameHash, (_, file) => file.Name)
            .RuleFor(f => f.Tags, _ => [])
            .RuleFor(f => f.EncryptedDirectory, _ => dirFaker.Generate())
            .RuleFor(f => f.EncryptedDirectoryId, (_, file) => file.EncryptedDirectory?.Id ?? 0)
            .RuleFor(f => f.Created, _ => DateTime.UtcNow)
            .RuleFor(f => f.Modified, _ => DateTime.UtcNow);
    }

    private static Faker<UserGroup> GetUserGroupFaker(RefNotesContext? context) =>
        CreateBaseFaker<UserGroup>(context)
            .StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Name, f => f.Lorem.Sentence(5));

    private static Faker<UserGroupRole> GetUserGroupRoleFaker(RefNotesContext? context)
    {
        var userFaker = GetUserFaker(context);
        var groupFaker = GetUserGroupFaker(context);

        return CreateBaseFaker<UserGroupRole>(context)
            .StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.User, _ => userFaker.Generate())
            .RuleFor(g => g.UserId, (_, role) => role.User?.Id ?? 0)
            .RuleFor(g => g.UserGroup, _ => groupFaker.Generate())
            .RuleFor(g => g.UserGroupId, (_, role) => role.UserGroup?.Id ?? 0)
            .RuleFor(g => g.Role, f => f.PickRandom<UserGroupRoleType>());
    }

    private static Faker<EmailConfirmToken> GetEmailConfirmTokenFaker(RefNotesContext? context)
    {
        var userFaker = GetUserFaker(context);

        return CreateBaseFaker<EmailConfirmToken>(context)
            .StrictMode(true)
            .RuleFor(t => t.Id, _ => 0)
            .RuleFor(t => t.Value, _ => Guid.NewGuid().ToString())
            .RuleFor(t => t.User, _ => userFaker.Generate())
            .RuleFor(t => t.UserId, (_, token) => token.User?.Id)
            .RuleFor(t => t.CreatedAt, _ => DateTime.UtcNow)
            .RuleFor(t => t.ExpiresAt, _ => DateTime.UtcNow.AddHours(1));
    }

    private static Faker<DirectoryFavorite> GetDirectoryFavoriteFaker(RefNotesContext? context)
    {
        var dirFaker = GetDirectoryFaker(context);
        var userFaker = GetUserFaker(context);

        return CreateBaseFaker<DirectoryFavorite>(context)
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.EncryptedDirectory, _ => dirFaker.Generate())
            .RuleFor(f => f.EncryptedDirectoryId, (_, file) => file.EncryptedDirectory?.Id)
            .RuleFor(f => f.User, _ => userFaker.Generate())
            .RuleFor(f => f.UserId, (_, file) => file.User?.Id)
            .RuleFor(f => f.FavoriteDate, _ => DateTime.UtcNow);
    }

    private static Faker<FileFavorite> GetFileFavoriteFaker(RefNotesContext? context)
    {
        var fileFaker = GetFileFaker(context);
        var userFaker = GetUserFaker(context);

        return CreateBaseFaker<FileFavorite>(context)
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.EncryptedFile, _ => fileFaker.Generate())
            .RuleFor(f => f.EncryptedFileId, (_, file) => file.EncryptedFile?.Id)
            .RuleFor(f => f.User, _ => userFaker.Generate())
            .RuleFor(f => f.UserId, (_, file) => file.User?.Id)
            .RuleFor(f => f.FavoriteDate, _ => DateTime.UtcNow);
    }

    private static Faker<PasswordResetToken> GetPasswordResetTokenFaker(RefNotesContext? context)
    {
        var userFaker = GetUserFaker(context);

        return CreateBaseFaker<PasswordResetToken>(context)
            .StrictMode(true)
            .RuleFor(t => t.Id, _ => 0)
            .RuleFor(t => t.Value, _ => Guid.NewGuid().ToString())
            .RuleFor(t => t.User, _ => userFaker.Generate())
            .RuleFor(t => t.UserId, (_, token) => token.User?.Id)
            .RuleFor(t => t.CreatedAt, _ => DateTime.UtcNow)
            .RuleFor(t => t.ExpiresAt, _ => DateTime.UtcNow.AddHours(1));
    }

    private static Faker<PublicFile> GetPublicFileFaker(RefNotesContext? context)
    {
        var fileFaker = GetFileFaker(context);

        return CreateBaseFaker<PublicFile>(context)
            .CustomInstantiator(_ => new PublicFile("", 0))
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.UrlHash, f => f.Random.Hash())
            .RuleFor(f => f.EncryptedFile, _ => fileFaker.Generate())
            .RuleFor(f => f.EncryptedFileId, (_, file) => file.EncryptedFile?.Id)
            .RuleFor(f => f.State, _ => PublicFileState.Active)
            .RuleFor(f => f.Created, _ => DateTime.UtcNow);
    }

    private static Faker<PublicFileImage> GetPublicFileImageFaker(RefNotesContext? context)
    {
        var publicFileFaker = GetPublicFileFaker(context);
        var encryptedFileFaker = GetFileFaker(context);

        return CreateBaseFaker<PublicFileImage>(context)
            .CustomInstantiator(_ => new PublicFileImage(0, 0))
            .StrictMode(true)
            .RuleFor(i => i.Id, _ => 0)
            .RuleFor(i => i.PublicFile, _ => publicFileFaker.Generate())
            .RuleFor(i => i.PublicFileId, (_, image) => image.PublicFile?.Id)
            .RuleFor(i => i.EncryptedFile, _ => encryptedFileFaker.Generate())
            .RuleFor(i => i.EncryptedFileId, (_, image) => image.EncryptedFile?.Id);
    }

    private static Faker<FileTag> GetFileTagFaker(RefNotesContext? context)
    {
        var userFaker = GetUserFaker(context);
        var groupFaker = GetUserGroupFaker(context);

        return CreateBaseFaker<FileTag>(context)
            .StrictMode(true)
            .RuleFor(t => t.Id, 0)
            .RuleFor(t => t.Name, f => f.Lorem.Word() + Guid.CreateVersion7())
            .RuleFor(t => t.NameHash, (_, tag) => tag.Name)
            .RuleFor(t => t.Files, _ => [])
            .RuleFor(t => t.Owner, _ => userFaker.Generate())
            .RuleFor(t => t.OwnerId, (_, tag) => tag.Owner?.Id)
            .RuleFor(t => t.GroupOwner, _ => groupFaker.Generate())
            .RuleFor(t => t.GroupOwnerId, (_, tag) => tag.GroupOwner?.Id);
    }

    private static Faker<SharedFileHash> GetSharedFileHashFaker(RefNotesContext? context)
    {
        var fileFaker = GetFileFaker(context);

        return CreateBaseFaker<SharedFileHash>(context)
            .StrictMode(true)
            .RuleFor(sf => sf.Id, 0)
            .RuleFor(sf => sf.Hash, _ => Guid.CreateVersion7().ToString())
            .RuleFor(sf => sf.EncryptedFile, _ => fileFaker.Generate())
            .RuleFor(sf => sf.CreatedAt, _ => DateTime.UtcNow)
            .RuleFor(sf => sf.IsDeleted, _ => false);
    }

    private static Faker<SharedFile> GetSharedFileFaker(RefNotesContext? context)
    {
        var fileFaker = GetFileFaker(context);
        var dirFaker = GetDirectoryFaker(context);

        return CreateBaseFaker<SharedFile>(context)
            .StrictMode(true)
            .RuleFor(sf => sf.Id, 0)
            .RuleFor(sf => sf.SharedEncryptedFile, _ => fileFaker.Generate())
            .RuleFor(sf => sf.SharedEncryptedFileId, (_, sharedFile) => sharedFile.SharedEncryptedFile?.Id)
            .RuleFor(sf => sf.SharedToDirectory, _ => dirFaker.Generate())
            .RuleFor(sf => sf.SharedToDirectoryId, (_, sharedFile) => sharedFile.SharedToDirectory?.Id)
            .RuleFor(sf => sf.Created, _ => DateTime.UtcNow);
    }
}
