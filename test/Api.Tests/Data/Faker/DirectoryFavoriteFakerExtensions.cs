using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class DirectoryFavoriteFakerExtensions
{
    public static Faker<DirectoryFavorite> ForUser(this Faker<DirectoryFavorite> faker, User user)
        => faker.RuleFor(f => f.User, _ => user);

    public static Faker<DirectoryFavorite> ForDirectory(this Faker<DirectoryFavorite> faker, EncryptedDirectory dir)
        => faker.RuleFor(f => f.EncryptedDirectory, _ => dir);
    
    public static Faker<DirectoryFavorite> ForDirectory(this Faker<DirectoryFavorite> faker, Faker<EncryptedDirectory> dirFaker)
        => faker.RuleFor(f => f.EncryptedDirectory, _ => dirFaker.Generate());
}
