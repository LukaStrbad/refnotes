using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public class PublicFileImageFakerImplementation : FakerImplementationBase<PublicFileImage>
{
    public PublicFileImageFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<PublicFileImage> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<PublicFileImage> CreateFaker()
    {
        var publicFileFaker = ResolveFaker<PublicFile>();
        var encryptedFileFaker = ResolveFaker<EncryptedFile>();

        return BaseFaker.CustomInstantiator(_ => new PublicFileImage(0, 0))
            .StrictMode(true)
            .RuleFor(i => i.Id, _ => 0)
            .RuleFor(i => i.PublicFile, _ => publicFileFaker.Generate())
            .RuleFor(i => i.PublicFileId, (_, image) => image.PublicFile?.Id)
            .RuleFor(i => i.EncryptedFile, _ => encryptedFileFaker.Generate())
            .RuleFor(i => i.EncryptedFileId, (_, image) => image.EncryptedFile?.Id);
    }
}
