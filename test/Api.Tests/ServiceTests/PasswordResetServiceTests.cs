using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests;

public sealed class PasswordResetServiceTests
{
    private readonly PasswordResetService _service;
    private readonly FakerResolver _fakerResolver;
    private readonly RefNotesContext _context;
    
    private readonly User _defaultUser;

    public PasswordResetServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<PasswordResetService>().WithDb(dbFixture).WithFakers()
            .CreateServiceProvider();
        
        _service = serviceProvider.GetRequiredService<PasswordResetService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        
        _defaultUser = _fakerResolver.Get<User>().Generate();
    }

    [Fact]
    public async Task GenerateToken_GeneratesToken()
    {
        var generatedToken = await _service.GenerateToken(_defaultUser.Id);

        var tokenEntity = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.UserId == _defaultUser.Id,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tokenEntity);
        Assert.Equal(generatedToken, tokenEntity.Value);
        // Assert expiration is 15 minutes from now
        Assert.InRange(tokenEntity.ExpiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task GenerateToken_DeletesExistingTokens()
    {
        _fakerResolver.Get<PasswordResetToken>().ForUser(_defaultUser).Generate();

        var generatedToken = await _service.GenerateToken(_defaultUser.Id);

        var tokenEntities = await _context.PasswordResetTokens
            .Where(x => x.UserId == _defaultUser.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(tokenEntities);
        Assert.Equal(generatedToken, tokenEntities[0].Value);
    }

    [Fact]
    public async Task ValidateToken_ReturnsTrueForValidToken()
    {
        var token = _fakerResolver.Get<PasswordResetToken>().ForUser(_defaultUser).Generate();

        var isValid = await _service.ValidateToken(_defaultUser.Id, token.Value);

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateToken_ReturnsFalseForInvalidToken()
    {
        var token = _fakerResolver.Get<PasswordResetToken>().ForUser(_defaultUser).WithExpiredToken().Generate();

        var isValid = await _service.ValidateToken(_defaultUser.Id, token.Value);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateToken_ReturnsFalseForWrongUser()
    {
        var token = _fakerResolver.Get<PasswordResetToken>().ForUser(_defaultUser).Generate();
        var otherUser = _fakerResolver.Get<User>().Generate();

        var isValid = await _service.ValidateToken(otherUser.Id, token.Value);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateToken_ReturnsFalseForWrongToken()
    {
        _fakerResolver.Get<PasswordResetToken>().ForUser(_defaultUser).Generate();

        var isValid = await _service.ValidateToken(_defaultUser.Id, "wrong-token");

        Assert.False(isValid);
    }
}
