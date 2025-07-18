using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.ServiceTests;

public sealed class PasswordResetServiceTests
{
    [Theory, AutoData]
    public async Task GenerateToken_GeneratesToken(Sut<PasswordResetService> sut)
    {
        var generatedToken = await sut.Value.GenerateToken(sut.DefaultUser.Id);
        
        var tokenEntity = await sut.Context.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.UserId == sut.DefaultUser.Id, cancellationToken: TestContext.Current.CancellationToken);
        
        Assert.NotNull(tokenEntity);
        Assert.Equal(generatedToken, tokenEntity.Value);
        // Assert expiration is 15 minutes from now
        Assert.InRange(tokenEntity.ExpiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }
    
    [Theory, AutoData]
    public async Task GenerateToken_DeletesExistingTokens(
        Sut<PasswordResetService> sut,
        PasswordResetTokenFakerImplementation tokenFaker)
    {
        tokenFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();

        var generatedToken = await sut.Value.GenerateToken(sut.DefaultUser.Id);

        var tokenEntities = await sut.Context.PasswordResetTokens
            .Where(x => x.UserId == sut.DefaultUser.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(tokenEntities);
        Assert.Equal(generatedToken, tokenEntities[0].Value);
    }
    
    [Theory, AutoData]
    public async Task ValidateToken_ReturnsTrueForValidToken(
        Sut<PasswordResetService> sut,
        PasswordResetTokenFakerImplementation tokenFaker)
    {
        var token = tokenFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();

        var isValid = await sut.Value.ValidateToken(sut.DefaultUser.Id, token.Value);

        Assert.True(isValid);
    }

    [Theory, AutoData]
    public async Task ValidateToken_ReturnsFalseForInvalidToken(
        Sut<PasswordResetService> sut,
        PasswordResetTokenFakerImplementation tokenFaker)
    {
        var token = tokenFaker.CreateFaker().ForUser(sut.DefaultUser).WithExpiredToken().Generate();
        
        var isValid = await sut.Value.ValidateToken(sut.DefaultUser.Id, token.Value);
        
        Assert.False(isValid);
    }
    
    [Theory, AutoData]
    public async Task ValidateToken_ReturnsFalseForWrongUser(
        Sut<PasswordResetService> sut,
        PasswordResetTokenFakerImplementation tokenFaker,
        UserFakerImplementation userFaker)
    {
        var token = tokenFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();
        var otherUser = userFaker.CreateFaker().Generate();
        
        var isValid = await sut.Value.ValidateToken(otherUser.Id, token.Value);
        
        Assert.False(isValid);
    }
    
    [Theory, AutoData]
    public async Task ValidateToken_ReturnsFalseForWrongToken(
        Sut<PasswordResetService> sut,
        PasswordResetTokenFakerImplementation tokenFaker)
    {
        tokenFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();
        
        var isValid = await sut.Value.ValidateToken(sut.DefaultUser.Id, "wrong-token");
        
        Assert.False(isValid);
    }
}
