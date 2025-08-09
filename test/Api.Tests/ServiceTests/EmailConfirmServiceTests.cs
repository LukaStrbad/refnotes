using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests;

public sealed class EmailConfirmServiceTests
{
    private readonly EmailConfirmService _service;
    private readonly RefNotesContext _context;
    private readonly FakerResolver _fakerResolver;

    private readonly User _defaultUser;

    public EmailConfirmServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<EmailConfirmService>().WithDb(dbFixture).WithFakers()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<EmailConfirmService>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        
        _defaultUser = _fakerResolver.Get<User>().Generate();
    }

    [Fact]
    public async Task GenerateToken_GenerateConfirmToken()
    {
        var generatedToken = await _service.GenerateToken(_defaultUser.Id);

        var tokenEntity = await _context.EmailConfirmTokens
            .FirstOrDefaultAsync(x => x.UserId == _defaultUser.Id,
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tokenEntity);
        Assert.Equal(generatedToken, tokenEntity.Value);
        // Assert expiration is 60 minutes from now
        Assert.InRange(tokenEntity.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task GenerateToken_DeletesExistingTokens()
    {
        _fakerResolver.Get<EmailConfirmToken>().ForUser(_defaultUser).Generate();

        var generatedToken = await _service.GenerateToken(_defaultUser.Id);

        var tokenEntities = await _context.EmailConfirmTokens
            .Where(x => x.UserId == _defaultUser.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(tokenEntities);
        Assert.Equal(generatedToken, tokenEntities[0].Value);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsTrue_WhenEmailConfirmed()
    {
        var user = _fakerResolver.Get<User>().WithUnconfirmedEmail().Generate();
        var token = _fakerResolver.Get<EmailConfirmToken>().ForUser(user).Generate();

        var (returnedUser, success) = await _service.ConfirmEmail(token.Value);

        await _context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.True(success);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(user.Id, returnedUser?.Id);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsFalse_WhenTokenIsExpired()
    {
        var user = _fakerResolver.Get<User>().WithUnconfirmedEmail().Generate();
        var token = _fakerResolver.Get<EmailConfirmToken>().ForUser(user).WithExpiredToken().Generate();

        var (returnedUser, success) = await _service.ConfirmEmail(token.Value);

        await _context.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.False(success);
        Assert.False(user.EmailConfirmed);
        Assert.Null(returnedUser);
    }
}
