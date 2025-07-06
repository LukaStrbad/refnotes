using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public class FileFavoriteFakerImplementation : FakerImplementationBase<FileFavorite>
{
    public FileFavoriteFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<FileFavorite> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<FileFavorite> CreateFaker()
    {
        var encryptedFileFaker = ResolveFaker<EncryptedFile>();
        var userFaker = ResolveFaker<User>();

        return BaseFaker.StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.EncryptedFile, _ => encryptedFileFaker.Generate())
            .RuleFor(f => f.EncryptedFileId, (_, file) => file.EncryptedFile?.Id)
            .RuleFor(f => f.User, _ => userFaker.Generate())
            .RuleFor(f => f.UserId, (_, file) => file.User?.Id)
            .RuleFor(f => f.FavoriteDate, _ => DateTime.UtcNow);
    }
}
