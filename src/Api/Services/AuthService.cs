using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Exceptions;
using Api.Model;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

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
    Task<Tokens> Register(RegisterUserRequest newUser);

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

    /// <summary>
    /// Bypasses the login flow and creates a new access token and refresh token for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>New access and refresh tokens</returns>
    Task<Tokens> ForceLogin(int userId);

    /// <summary>
    /// Verifies the password of a user
    /// </summary>
    /// <param name="credentials">Username and password</param>
    /// <returns>True if password matches, false otherwise</returns>
    Task<bool> VerifyPassword(UserCredentials credentials);
}

public class AuthService : IAuthService
{
    private readonly RefNotesContext _context;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(RefNotesContext context, ILogger<AuthService> logger, AppSettings appSettings)
    {
        _context = context;

        _logger = logger;
        _appSettings = appSettings;
    }

    public async Task<Tokens> Login(UserCredentials credentials)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == credentials.Username);
        if (user is null)
        {
            _logger.LogWarning("User {Username} not found", StringSanitizer.SanitizeLog(credentials.Username));
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
                await _context.SaveChangesAsync();
                break;
        }

        _logger.LogInformation("User {Username} logged in", StringSanitizer.SanitizeLog(credentials.Username));

        var tokens = await CreateAccessAndRefreshTokens(user);
        return tokens;
    }

    public async Task<Tokens> Register(RegisterUserRequest userRequest)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userRequest.Username);
        if (existingUser is not null)
        {
            _logger.LogWarning("User {Username} already exists", StringSanitizer.SanitizeLog(userRequest.Username));
            throw new UserExistsException("User already exists");
        }

        var newUser = userRequest.ToUser();

        // Hash the password before saving
        var passwordHasher = new PasswordHasher<UserCredentials>();
        newUser.Password =
            passwordHasher.HashPassword(new UserCredentials(newUser.Username, newUser.Password), newUser.Password);
        await _context.AddAsync(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Username} registered", StringSanitizer.SanitizeLog(newUser.Username));

        return await CreateAccessAndRefreshTokens(newUser);
    }

    public async Task<Tokens> RefreshAccessToken(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
        var name = principal.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (user is null)
        {
            _logger.LogWarning("User {Username} not found", name);
            throw new UserNotFoundException("No user found");
        }

        var savedRefreshToken =
            await _context.UserRefreshTokens.FirstOrDefaultAsync(t =>
                t.Username == user.Username && t.RefreshToken == refreshToken);
        if (savedRefreshToken is null || savedRefreshToken.IsExpired)
        {
            throw new RefreshTokenInvalid("Refresh token is invalid or expired");
        }

        _context.UserRefreshTokens.Remove(savedRefreshToken);
        await _context.SaveChangesAsync();

        var newTokens = await CreateAccessAndRefreshTokens(user);
        return newTokens;
    }

    public async Task<Tokens> ForceLogin(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new UserNotFoundException("User not found");

        _logger.LogInformation("Forcing login for user {User}", StringSanitizer.SanitizeLog(user.Username));
        var tokens = await CreateAccessAndRefreshTokens(user);
        return tokens;
    }

    public async Task<bool> VerifyPassword(UserCredentials credentials)
    {
        var dbPassword = await _context.Users.Where(u => u.Username == credentials.Username).Select(u => u.Password)
            .FirstOrDefaultAsync();
        if (dbPassword is null)
            throw new UserNotFoundException("User not found");

        var passwordHasher = new PasswordHasher<UserCredentials>();
        var result = passwordHasher.VerifyHashedPassword(credentials, dbPassword, credentials.Password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
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

        ci.AddClaims(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

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
            IssuerSigningKey = new SymmetricSecurityKey(_appSettings.JwtPrivateKeyBytes),
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

    private async Task<Tokens> CreateAccessAndRefreshTokens(User user)
    {
        var handler = new JwtSecurityTokenHandler();

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_appSettings.JwtPrivateKeyBytes),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.Add(_appSettings.AccessTokenExpiry),
            Subject = GenerateClaims(user)
        };

        var token = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(token);
        var refreshToken = TokenService.GenerateRefreshToken();
        var tokens = new Tokens(accessToken, refreshToken);

        await _context.AddAsync(new UserRefreshToken
        {
            Username = user.Username,
            RefreshToken = tokens.RefreshToken.Token,
            ExpiryTime = tokens.RefreshToken.ExpiryTime
        });
        await _context.SaveChangesAsync();

        return tokens;
    }
}
