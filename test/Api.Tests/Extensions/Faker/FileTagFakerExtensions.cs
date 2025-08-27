using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class FileTagFakerExtensions
{
    public static Faker<FileTag> WithName(this Faker<FileTag> faker, string name)
        => faker.RuleFor(t => t.Name, name);

    public static Faker<FileTag> ForFiles(this Faker<FileTag> faker, params List<EncryptedFile> files)
        => faker.RuleFor(t => t.Files, files);

    public static Faker<FileTag> ForUser(this Faker<FileTag> faker, User owner)
        => faker.RuleFor(t => t.Owner, owner);

    public static Faker<FileTag> ForGroup(this Faker<FileTag> faker, UserGroup group)
        => faker.RuleFor(t => t.GroupOwner, group);

    public static Faker<FileTag> ForUserOrGroup(this Faker<FileTag> faker, User user, UserGroup? group)
        => group is null ? faker.ForUser(user) : faker.ForGroup(group);
}
