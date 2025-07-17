using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public class EmailConfirmTokenFakerImplementation : FakerImplementationBase<EmailConfirmToken>
{
    public EmailConfirmTokenFakerImplementation(Dictionary<Type, FakerImplementationBase> fakerImplementations,
        Faker<EmailConfirmToken> baseFaker) : base(fakerImplementations, baseFaker)
    {
    }

    public override Faker<EmailConfirmToken> CreateFaker()
    {
        var userFaker = ResolveFaker<User>();

        return BaseFaker.StrictMode(true)
            .RuleFor(t => t.Id, _ => 0)
            .RuleFor(t => t.Value, _ => Guid.NewGuid().ToString())
            .RuleFor(t => t.User, _ => userFaker.Generate())
            .RuleFor(t => t.UserId, (_, token) => token.User?.Id)
            .RuleFor(t => t.CreatedAt, _ => DateTime.UtcNow)
            .RuleFor(t => t.ExpiresAt, _ => DateTime.UtcNow.AddHours(1));
    }
}
