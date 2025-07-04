using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class EncryptedFileFakerExtensions
{
    private static readonly string[] ImageExtensions = ["jpg", "png", "gif"];

    public static Faker<EncryptedFile> WithName(this Faker<EncryptedFile> faker, string name)
        => faker.RuleFor(f => f.Name, _ => name);

    public static Faker<EncryptedFile> WithNameExtension(this Faker<EncryptedFile> faker, string extension)
        => faker.RuleFor(f => f.Name, f => f.System.FileName(extension));

    public static Faker<EncryptedFile> AsImage(this Faker<EncryptedFile> faker)
        => faker.RuleFor(f => f.Name, f => f.System.FileName(f.PickRandom(ImageExtensions)));

    public static Faker<EncryptedFile> ForDir(this Faker<EncryptedFile> faker, EncryptedDirectory dir)
        => faker.RuleFor(f => f.EncryptedDirectory, _ => dir);
}
