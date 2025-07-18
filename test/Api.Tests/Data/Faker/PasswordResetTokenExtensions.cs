using Data.Model;
using Bogus;

namespace Api.Tests.Data.Faker;

public static class PasswordResetTokenExtensions
{
    public static Faker<PasswordResetToken> ForUser(this Faker<PasswordResetToken> faker, User user)
        => faker.RuleFor(t => t.User, _ => user);
    
    public static Faker<PasswordResetToken> WithExpiredToken(this Faker<PasswordResetToken> faker)
        => faker.RuleFor(t => t.ExpiresAt, _ => DateTime.UtcNow.AddDays(-1));
}
