using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Gets the cookie options for the refresh token.
    /// </summary>
    private static readonly CookieOptions HttpOnlyCookieOptions = new()
    {
        Domain = "localhost",
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        HttpOnly = true,
        Secure = false
    };

    [HttpPost("login")]
    [ProducesResponseType<Ok<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<NotFound>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> Login(UserCredentials credentials)
    {
        try
        {
            var tokens = await authService.Login(credentials);
            AddRefreshTokenCookie(tokens);
            return Ok(tokens.AccessToken);
        }
        catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("register")]
    [ProducesResponseType<Ok<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<BadRequest>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> Register(User newUser)
    {
        try
        {
            var tokens = await authService.Register(newUser);
            AddRefreshTokenCookie(tokens);
            return Ok(tokens.AccessToken);
        } catch (UserExistsException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("refreshAccessToken")]
    [ProducesResponseType<Ok<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<NotFound>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> RefreshToken()
    {
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];
        using var sr = new StreamReader(Request.Body);
        string accessToken;
        try
        {
            accessToken = await sr.ReadToEndAsync();
        } catch
        {
            return BadRequest("Error reading access token from request body");
        }

        if (refreshToken is null)
        {
            return BadRequest("No refresh token provided");
        }

        try
        {
            var tokens = await authService.RefreshAccessToken(accessToken, refreshToken);
            AddRefreshTokenCookie(tokens);
            return Ok(tokens.AccessToken);
        }
        catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (RefreshTokenInvalid e)
        {
            return BadRequest(e.Message);
        }
        catch (SecurityTokenMalformedException e)
        {
            return BadRequest(e.Message);
        }
    }

    private void AddRefreshTokenCookie(Tokens tokens)
    {
        var options = HttpOnlyCookieOptions;
        options.Expires = tokens.RefreshToken.ExpiryTime;
        HttpContext.Response.Cookies.Append("refreshToken", tokens.RefreshToken.Token, HttpOnlyCookieOptions);
    }
}