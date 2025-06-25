using System.Security.Cryptography;
using Api.Exceptions;
using Api.Model;
using Data.Model;

namespace Api.Services;

public sealed class TokenService
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

    /// <summary>
    /// Generates a new <see cref="GroupAccessCode"/> for the specified user and user group
    /// with a defined expiry time. Ensures the expiry time does not exceed a pre-defined limit.
    /// </summary>
    /// <param name="sender">The user generating the group access code.</param>
    /// <param name="group">The user group for which the access code is being created.</param>
    /// <param name="expiryTime">The expiration time for the access code.</param>
    /// <returns>A new instance of <see cref="GroupAccessCode"/> containing the generated access code details.</returns>
    /// <exception cref="ExpiryTimeTooLongException">
    /// Thrown when the specified expiry time exceeds the maximum allowed limit.
    /// </exception>
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