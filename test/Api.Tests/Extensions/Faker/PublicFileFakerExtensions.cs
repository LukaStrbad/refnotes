using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class PublicFileFakerExtensions
{
    public static Faker<PublicFile> ForFile(this Faker<PublicFile> faker, EncryptedFile encryptedFile)
        => faker.RuleFor(f => f.EncryptedFile, _ => encryptedFile);
    
    public static Faker<PublicFile> Inactive(this Faker<PublicFile> faker)
        => faker.RuleFor(f => f.State, PublicFileState.Inactive);
}
