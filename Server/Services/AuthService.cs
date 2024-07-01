using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Server.Model;

namespace Server.Services;

public class AuthService
{
    private readonly byte[] _privateKey = Configuration.AppConfig.JwtPrivateKeyBytes;
    
    public string CreateToken(User user)
    {
        var handler = new JwtSecurityTokenHandler();

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_privateKey),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = GenerateClaims(user),
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
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
}