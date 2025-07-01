using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class PublicFileFakerExtensions
{
    public static Faker<PublicFile> ForFile(this Faker<PublicFile> faker, EncryptedFile encryptedFile)
        => faker.RuleFor(f => f.EncryptedFile, _ => encryptedFile);
}
