using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class SharedFileFakerExtensions
{
    public static Faker<SharedFile> ForFile(this Faker<SharedFile> faker, EncryptedFile encryptedFile)
        => faker.RuleFor(f => f.SharedEncryptedFile, encryptedFile);

    public static Faker<SharedFile> SharedToDir(this Faker<SharedFile> faker, EncryptedDirectory dir)
        => faker.RuleFor(f => f.SharedToDirectory, dir);
}
