using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class UserFakerExtensions
{
    public static Faker<User> WithConfirmedEmail(this Faker<User> faker)
        => faker.RuleFor(u => u.EmailConfirmed, _ => true);

    public static Faker<User> WithUnconfirmedEmail(this Faker<User> faker)
        => faker.RuleFor(u => u.EmailConfirmed, _ => false);

    public static Faker<User> WithRoles(this Faker<User> faker, params string[] roles)
        => faker.RuleFor(u => u.Roles, _ => roles);
}
