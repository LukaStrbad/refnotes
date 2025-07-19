using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class EmailConfirmTokenExtensions
{
    public static Faker<EmailConfirmToken> ForUser(this Faker<EmailConfirmToken> faker, User user)
        => faker.RuleFor(t => t.User, _ => user);
    
    public static Faker<EmailConfirmToken> WithExpiredToken(this Faker<EmailConfirmToken> faker)
        => faker.RuleFor(t => t.ExpiresAt, _ => DateTime.UtcNow.AddDays(-1));
}
