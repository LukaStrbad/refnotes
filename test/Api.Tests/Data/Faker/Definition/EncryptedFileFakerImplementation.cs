using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public sealed class EncryptedFileFakerImplementation : FakerImplementationBase<EncryptedFile>
{
    public EncryptedFileFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<EncryptedFile> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<EncryptedFile> CreateFaker()
    {
        var encryptedDirectoryFaker = ResolveFaker<EncryptedDirectory>();
        return BaseFaker.CustomInstantiator(_ => new EncryptedFile("", "", ""))
            .StrictMode(true)
            .RuleFor(f => f.Id, f => 0)
            .RuleFor(f => f.FilesystemName, f => f.System.FileName())
            .RuleFor(f => f.Name, f => f.System.FileName())
            .RuleFor(f => f.NameHash, (_, file) => file.Name)
            .RuleFor(f => f.Tags, _ => [])
            .RuleFor(f => f.EncryptedDirectory, _ => encryptedDirectoryFaker.Generate())
            .RuleFor(f => f.EncryptedDirectoryId, (_, file) => file.EncryptedDirectory?.Id ?? 0)
            .RuleFor(f => f.Created, _ => DateTime.UtcNow)
            .RuleFor(f => f.Modified, _ => DateTime.UtcNow);
    }
}
