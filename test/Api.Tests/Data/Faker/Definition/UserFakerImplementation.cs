using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public sealed class UserFakerImplementation : FakerImplementationBase<User>
{
    public UserFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<User> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<User> CreateFaker()
    {
        return BaseFaker.CustomInstantiator(_ => new User(0, "", "", "", ""))
            .StrictMode(true)
            .RuleFor(u => u.Id, f => 0)
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password())
            .RuleFor(u => u.Roles, _ => []);
    }
}
