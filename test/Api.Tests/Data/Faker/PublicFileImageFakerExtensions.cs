using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class PublicFileImageFakerExtensions
{
    public static Faker<PublicFileImage> ForPublicFile(this Faker<PublicFileImage> faker, PublicFile publicFile)
        => faker.RuleFor(i => i.PublicFile, _ => publicFile);
}
