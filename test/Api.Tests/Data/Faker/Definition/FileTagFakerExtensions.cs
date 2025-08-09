using Bogus;
using Data.Model;

namespace Api.Tests.Data.Faker.Definition;

public static class FileTagFakerExtensions
{
    public static Faker<FileTag> WithName(this Faker<FileTag> faker, string name)
        => faker.RuleFor(t => t.Name, name);
    
    public static Faker<FileTag> ForFiles(this Faker<FileTag> faker, params List<EncryptedFile> files)
        => faker.RuleFor(t => t.Files, files);
}
