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
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly string _cookieDomain;
    private readonly bool _cookieSecure;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;

        var cookieDomain = configuration["CookieDomain"];

        _cookieDomain = cookieDomain ?? throw new Exception("CookieDomain not set in configuration");
        _cookieSecure = configuration.GetValue<bool?>("CookieSecure") ?? false;
    }

    private CookieOptions GetCookieOptions(bool httpOnly, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            Domain = _cookieDomain,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            HttpOnly = httpOnly,
            Secure = _cookieSecure
        };

        if (expires is not null)
        {
            options.Expires = expires;
        }

        return options;
    }

    [HttpPost("login")]
    [ProducesResponseType<Ok>(StatusCodes.Status200OK)]
    [ProducesResponseType<NotFound>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<UnauthorizedHttpResult>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> Login(UserCredentials credentials)
    {
        try
        {
            var tokens = await _authService.Login(credentials);
            AddTokenCookies(tokens);
            return Ok();
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
    [ProducesResponseType<Ok>(StatusCodes.Status200OK)]
    [ProducesResponseType<BadRequest>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> Register(User newUser)
    {
        try
        {
            var tokens = await _authService.Register(newUser);
            AddTokenCookies(tokens);
            return Ok();
        }
        catch (UserExistsException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("refreshAccessToken")]
    [ProducesResponseType<Ok>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<NotFound>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> RefreshToken()
    {
        var accessToken = HttpContext.Request.Cookies["accessToken"];
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];
        if (accessToken is null)
        {
            return BadRequest("No access token provided");
        }

        if (refreshToken is null)
        {
            return BadRequest("No refresh token provided");
        }

        try
        {
            var tokens = await _authService.RefreshAccessToken(accessToken, refreshToken);
            AddTokenCookies(tokens);
            return Ok();
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

    private void AddTokenCookies(Tokens tokens)
    {
        // Set an access token cookie (set the same expiration time as the refresh token)
        HttpContext.Response.Cookies.Append("accessToken", tokens.AccessToken, GetCookieOptions(false, tokens.RefreshToken.ExpiryTime));
        // Set refresh token cookie   
        HttpContext.Response.Cookies.Append("refreshToken", tokens.RefreshToken.Token, GetCookieOptions(true, tokens.RefreshToken.ExpiryTime));
    }
}