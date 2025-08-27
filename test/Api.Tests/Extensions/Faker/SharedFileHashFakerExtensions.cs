using Bogus;
using Data.Model;

namespace Api.Tests.Extensions.Faker;

public static class SharedFileHashFakerExtensions
{
    public static Faker<SharedFileHash> AsDeleted(this Faker<SharedFileHash> faker)
        => faker.RuleFor(sf => sf.IsDeleted, _ => true);
}
