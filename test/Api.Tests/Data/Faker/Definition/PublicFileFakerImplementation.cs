using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public sealed class PublicFileFakerImplementation : FakerImplementationBase<PublicFile>
{
    public PublicFileFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<PublicFile> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    protected override Faker<PublicFile> CreateFaker()
    {
        var encryptedFileFaker = ResolveFaker<EncryptedFile>();
        return BaseFaker.CustomInstantiator(_ => new PublicFile("", 0))
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.UrlHash, f => f.Random.Hash())
            .RuleFor(f => f.EncryptedFile, _ => encryptedFileFaker.Generate())
            .RuleFor(f => f.EncryptedFileId, (_, file) => file.Id)
            .RuleFor(f => f.State, _ => PublicFileState.Active)
            .RuleFor(f => f.Created, _ => DateTime.UtcNow);
    }
}
