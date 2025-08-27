using Api.Controllers.Base;
using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Services.Schedulers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : AuthControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailScheduler _emailScheduler;
    private readonly IEmailConfirmService _emailConfirmService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IAppDomainService appDomainService, AppSettings appSettings,
        IEmailScheduler emailScheduler, IEmailConfirmService emailConfirmService, IUserService userService) : base(
        appDomainService, appSettings)
    {
        _authService = authService;
        _emailScheduler = emailScheduler;
        _emailConfirmService = emailConfirmService;
        _userService = userService;
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
    [ProducesResponseType<ErrorCodeResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ModelStateDictionary>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> Register([FromBody] RegisterUserRequest newUser, [FromQuery] string? lang)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var tokens = await _authService.Register(newUser);
            AddTokenCookies(tokens);
            var user = await _userService.GetByUsername(newUser.Username);
            var token = await _emailConfirmService.GenerateToken(user.Id);
            await _emailScheduler.ScheduleVerificationEmail(newUser.Email, newUser.Name, token, lang ?? "en");
            return Ok();
        }
        catch (UserExistsException)
        {
            return Conflict(ErrorCodes.UserAlreadyExists);
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
}
