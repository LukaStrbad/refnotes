using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class UserGroupRoleFakerExtensions
{
    public static Faker<UserGroupRole> ForUser(this Faker<UserGroupRole> faker, User user)
        => faker.RuleFor(r => r.User, user);

    public static Faker<UserGroupRole> ForGroup(this Faker<UserGroupRole> faker, UserGroup group)
        => faker.RuleFor(r => r.UserGroup, group);
    
    public static Faker<UserGroupRole> WithRole(this Faker<UserGroupRole> faker, UserGroupRoleType role)
        => faker.RuleFor(r => r.Role, role);
}
