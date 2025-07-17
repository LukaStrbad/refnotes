using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class UserFakerExtensions
{
    public static Faker<User> WithConfirmedEmail(this Faker<User> faker)
        => faker.RuleFor(u => u.EmailConfirmed, _ => true);
    
    public static Faker<User> WithUnconfirmedEmail(this Faker<User> faker)
        => faker.RuleFor(u => u.EmailConfirmed, _ => false);
}
