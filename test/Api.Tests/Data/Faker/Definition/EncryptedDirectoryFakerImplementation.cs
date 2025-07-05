using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public sealed class EncryptedDirectoryFakerImplementation : FakerImplementationBase<EncryptedDirectory>
{
    public EncryptedDirectoryFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<EncryptedDirectory> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<EncryptedDirectory> CreateFaker()
    {
        var userFaker = ResolveFaker<User>();
        return BaseFaker.StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Path, f => f.System.DirectoryPath())
            .RuleFor(g => g.Files, _ => [])
            .RuleFor(g => g.Directories, _ => [])
            .RuleFor(g => g.Parent, _ => null)
            .RuleFor(g => g.Owner, _ => userFaker.Generate())
            .RuleFor(g => g.OwnerId, (_, dir) => dir.Owner?.Id)
            .RuleFor(g => g.Group, _ => null)
            .RuleFor(d => d.GroupId, (_, dir) => dir.Group?.Id);
    }
}
