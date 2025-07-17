using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.ServiceTests;

public sealed class EmailConfirmServiceTests
{
    [Theory, AutoData]
    public async Task GenerateToken_GenerateConfirmToken(Sut<EmailConfirmService> sut)
    {
        var generatedToken = await sut.Value.GenerateToken(sut.DefaultUser.Id);

        var tokenEntity = await sut.Context.EmailConfirmTokens
            .FirstOrDefaultAsync(x => x.UserId == sut.DefaultUser.Id,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tokenEntity);
        Assert.Equal(generatedToken, tokenEntity.Value);
        // Assert expiration is 60 minutes from now
        Assert.InRange(tokenEntity.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Theory, AutoData]
    public async Task GenerateToken_DeletesExistingTokens(
        Sut<EmailConfirmService> sut,
        EmailConfirmTokenFakerImplementation tokenFaker)
    {
        tokenFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();

        var generatedToken = await sut.Value.GenerateToken(sut.DefaultUser.Id);

        var tokenEntities = await sut.Context.EmailConfirmTokens
            .Where(x => x.UserId == sut.DefaultUser.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(tokenEntities);
        Assert.Equal(generatedToken, tokenEntities[0].Value);
    }

    [Theory, AutoData]
    public async Task ConfirmEmail_ReturnsTrue_WhenEmailConfirmed(
        Sut<EmailConfirmService> sut,
        UserFakerImplementation userFaker,
        EmailConfirmTokenFakerImplementation tokenFaker)
    {
        var user = userFaker.CreateFaker().WithUnconfirmedEmail().Generate();
        var token = tokenFaker.CreateFaker().ForUser(user).Generate();

        var result = await sut.Value.ConfirmEmail(token.Value, user.Id);

        await sut.Context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.True(result);
        Assert.True(user.EmailConfirmed);
    }
    
    [Theory, AutoData]
    public async Task ConfirmEmail_ReturnsFalse_WhenTokenIsExpired(
        Sut<EmailConfirmService> sut,
        UserFakerImplementation userFaker,
        EmailConfirmTokenFakerImplementation tokenFaker)
    {
        var user = userFaker.CreateFaker().WithUnconfirmedEmail().Generate();
        var token = tokenFaker.CreateFaker().ForUser(user).WithExpiredToken().Generate();

        var result = await sut.Value.ConfirmEmail(token.Value, user.Id);

        await sut.Context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.False(result);
        Assert.False(user.EmailConfirmed);
    }
}
