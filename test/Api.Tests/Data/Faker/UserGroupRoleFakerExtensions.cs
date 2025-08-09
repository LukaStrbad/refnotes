using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class UserGroupRoleFakerExtensions
{
    public static Faker<UserGroupRole> ForUser(this Faker<UserGroupRole> faker, User user)
        => faker.RuleFor(r => r.User, user);

    public static Faker<UserGroupRole> ForGroup(this Faker<UserGroupRole> faker, UserGroup group)
        => faker.RuleFor(r => r.UserGroup, group);
}
