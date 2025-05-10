using System.Security.Cryptography;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;

namespace Server.Services;

public class TokenService
{
    private const int RefreshTokenExpirationDays = 7;
    private const int MaxGroupAccessCodeExpiryTimeDays = 7;

    private static string GenerateRandomString(int length)
    {
        var randomNumber = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        return token;
    }

    public static RefreshToken GenerateRefreshToken()
    {
        var token = GenerateRandomString(32);
        var expiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays);
        return new RefreshToken(token, expiryTime);
    }

    public static GroupAccessCode GenerateGroupAccessCode(User sender, UserGroup group, DateTime expiryTime)
    {
        var maxExpiryTime = DateTime.UtcNow.AddDays(MaxGroupAccessCodeExpiryTimeDays);

        if (expiryTime > maxExpiryTime)
        {
            throw new ExpiryTimeTooLongException(
                $"Expiry time should be at most {MaxGroupAccessCodeExpiryTimeDays} days");
        }
        
        var accessCode = GenerateRandomString(64);
        var groupAccessCode = new GroupAccessCode
        {
            Group = group,
            Sender = sender,
            Value = accessCode,
            ExpiryTime = expiryTime
        };
        return groupAccessCode;
    }
}