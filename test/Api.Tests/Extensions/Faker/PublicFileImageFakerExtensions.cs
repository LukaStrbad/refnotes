using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class PublicFileImageFakerExtensions
{
    public static Faker<PublicFileImage> ForPublicFile(this Faker<PublicFileImage> faker, PublicFile publicFile)
        => faker.RuleFor(i => i.PublicFile, _ => publicFile);
}
