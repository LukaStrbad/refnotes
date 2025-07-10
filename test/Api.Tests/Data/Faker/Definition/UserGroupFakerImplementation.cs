using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public sealed class UserGroupFakerImplementation : FakerImplementationBase<UserGroup>
{
    public UserGroupFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<UserGroup> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<UserGroup> CreateFaker()
    {
        return BaseFaker.StrictMode(true)
            .RuleFor(g => g.Id, f => 0)
            .RuleFor(g => g.Name, f => f.Lorem.Sentence(5));
    }
}
