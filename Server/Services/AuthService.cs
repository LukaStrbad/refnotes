using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Exceptions;
using Server.Model;

namespace Server.Services;

public interface IAuthService
{
    /// <summary>
    /// Logs in a user and returns a new access token and refresh token.
    /// </summary>
    /// <param name="credentials">Username and password</param>
    /// <returns>New access and refresh tokens</returns>
    /// <exception cref="UserNotFoundException">Thrown when user doesn't exist</exception>
    /// <exception cref="UnauthorizedException">Thrown when user is found but password is wrong</exception>
    Task<Tokens> Login(UserCredentials credentials);

    /// <summary>
    /// Registers a new user and returns a new access token and refresh token.
    /// </summary>
    /// <param name="newUser">The new user to register</param>
    /// <returns>New access and refresh tokens</returns>
    /// <exception cref="UserExistsException">Thrown when a user with the same username already exists</exception>
    Task<Tokens> Register(User newUser);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="accessToken">Previous access token</param>
    /// <param name="refreshToken">Current refresh token</param>
    /// <returns>New access and refresh tokens</returns>
    /// <exception cref="UserNotFoundException">Throw when user doesn't exist</exception>
    /// <exception cref="RefreshTokenInvalid">Thrown when saved refresh token could not be found, or is expired</exception>
    /// <exception cref="SecurityTokenMalformedException">Thrown when provided access token is invalid</exception>
    Task<Tokens> RefreshAccessToken(string accessToken, string refreshToken);
}

public class AuthService(RefNotesContext context, AppConfiguration appConfig) : IAuthService
{
    private const int RefreshTokenExpirationDays = 7;
    private readonly byte[] _privateKey = appConfig.JwtPrivateKeyBytes;

    public async Task<Tokens> Login(UserCredentials credentials)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == credentials.Username);
        if (user is null)
        {
            throw new UserNotFoundException("User not found.");
        }

        var passwordHasher = new PasswordHasher<UserCredentials>();
        var result = passwordHasher.VerifyHashedPassword(credentials, user.Password, credentials.Password);

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (result)
        {
            case PasswordVerificationResult.Failed:
                throw new UnauthorizedException();
            case PasswordVerificationResult.SuccessRehashNeeded:
                user.Password = passwordHasher.HashPassword(credentials, credentials.Password);
                await context.SaveChangesAsync();
                break;
        }

        var tokens = await AddUserRefreshToken(user);
        return tokens;
    }

    public async Task<Tokens> Register(User newUser)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == newUser.Username);
        if (existingUser is not null)
        {
            throw new UserExistsException("User already exists");
        }
        newUser.Roles = [];

        var passwordHasher = new PasswordHasher<UserCredentials>();
        newUser.Password =
            passwordHasher.HashPassword(new UserCredentials(newUser.Username, newUser.Password), newUser.Password);
        await context.AddAsync(newUser);
        await context.SaveChangesAsync();

        return await AddUserRefreshToken(newUser);
    }

    public async Task<Tokens> RefreshAccessToken(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
        var name = principal.Identity?.Name;
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (user is null)
        {
            throw new UserNotFoundException("No user found");
        }

        var savedRefreshToken =
            await context.UserRefreshTokens.FirstOrDefaultAsync(t =>
                t.Username == user.Username && t.RefreshToken == refreshToken);
        if (savedRefreshToken is null || savedRefreshToken.IsExpired)
        {
            throw new RefreshTokenInvalid("Refresh token is invalid or expired");
        }

        context.UserRefreshTokens.Remove(savedRefreshToken);
        await context.SaveChangesAsync();

        var newTokens = await AddUserRefreshToken(user);
        return newTokens;
    }

    private Tokens CreateTokens(User user)
    {
        var handler = new JwtSecurityTokenHandler();

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_privateKey),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = GenerateClaims(user)
        };

        var token = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(token);
        var refreshToken = GenerateRefreshToken();
        return new Tokens(accessToken, refreshToken);
    }

    private static RefreshToken GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        var expiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays);
        return new RefreshToken(token, expiryTime);
    }

    private static ClaimsIdentity GenerateClaims(User user)
    {
        var ci = new ClaimsIdentity();

        ci.AddClaims([
            new Claim("id", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
        ]);

        ci.AddClaims(user.Roles?.Select(role => new Claim(ClaimTypes.Role, role)) ?? []);

        return ci;
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_privateKey),
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken is null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenMalformedException("Invalid token");
        }

        return principal;
    }

    private async Task<Tokens> AddUserRefreshToken(User user)
    {
        var tokens = CreateTokens(user);
        await context.AddAsync(new UserRefreshToken
        {
            Username = user.Username,
            RefreshToken = tokens.RefreshToken.Token,
            ExpiryTime = tokens.RefreshToken.ExpiryTime
        });
        await context.SaveChangesAsync();

        return tokens;
    }
}