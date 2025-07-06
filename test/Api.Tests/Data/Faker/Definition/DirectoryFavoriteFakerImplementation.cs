using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public class DirectoryFavoriteFakerImplementation : FakerImplementationBase<DirectoryFavorite>
{
    public DirectoryFavoriteFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<DirectoryFavorite> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<DirectoryFavorite> CreateFaker()
    {
        var encryptedDirectoryFaker = ResolveFaker<EncryptedDirectory>();
        var userFaker = ResolveFaker<User>();

        return BaseFaker.StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.EncryptedDirectory, _ => encryptedDirectoryFaker.Generate())
            .RuleFor(f => f.EncryptedDirectoryId, (_, file) => file.EncryptedDirectory?.Id)
            .RuleFor(f => f.User, _ => userFaker.Generate())
            .RuleFor(f => f.UserId, (_, file) => file.User?.Id)
            .RuleFor(f => f.FavoriteDate, _ => DateTime.UtcNow);
    }
}
