using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class FileFavoriteFakerExtensions
{
    public static Faker<FileFavorite> ForUser(this Faker<FileFavorite> faker, User user, Faker<EncryptedFile> fileFaker)
    {
        return faker.RuleFor(f => f.User, _ => user)
            .RuleFor(f => f.EncryptedFile, _ => fileFaker.Generate());
    }
    
    public static Faker<FileFavorite> ForUser(this Faker<FileFavorite> faker, User user)
        => faker.RuleFor(f => f.User, _ => user);

    public static Faker<FileFavorite> ForFile(this Faker<FileFavorite> faker, EncryptedFile encryptedFile)
        => faker.RuleFor(f => f.EncryptedFile, _ => encryptedFile);
}
