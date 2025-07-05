using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class EncryptedDirectoryFakerExtensions
{
    public static Faker<EncryptedDirectory> WithPath(this Faker<EncryptedDirectory> faker, string path)
        => faker.RuleFor(d => d.Path, _ => path);

    public static Faker<EncryptedDirectory> ForUser(this Faker<EncryptedDirectory> faker, User user)
        => faker.RuleFor(d => d.Owner, _ => user);

    public static Faker<EncryptedDirectory> ForGroup(this Faker<EncryptedDirectory> faker, UserGroup group)
        => faker.RuleFor(d => d.Group, _ => group)
            .RuleFor(d => d.Owner, _ => null);

    /// <summary>
    /// Assigns User as the owner if the group is null, otherwise assigns the group.
    /// </summary>
    /// <param name="faker">The current faker instance</param>
    /// <param name="user">The user</param>
    /// <param name="group">The group</param>
    public static Faker<EncryptedDirectory> ForUserOrGroup(this Faker<EncryptedDirectory> faker, User user,
        UserGroup? group)
        => group is null ? faker.ForUser(user) : faker.ForGroup(group);
}
