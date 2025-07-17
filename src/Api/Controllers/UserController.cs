using Api.Controllers.Base;
using Api.Services;
using Api.Services.Schedulers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class UserController : AuthControllerBase
{
    private readonly IUserService _userService;
    private readonly IEmailConfirmService _emailConfirmService;
    private readonly IAuthService _authService;
    private readonly IEmailScheduler _emailScheduler;

    public UserController(IUserService userService, IEmailConfirmService emailConfirmService, IAuthService authService,
        IAppDomainService appDomainService, AppSettings appSettings, IEmailScheduler emailScheduler) : base(appDomainService, appSettings)
    {
        _userService = userService;
        _emailConfirmService = emailConfirmService;
        _authService = authService;
        _emailScheduler = emailScheduler;
    }

    [HttpPost("confirmEmail/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmEmail(string token)
    {
        var (user, success) = await _emailConfirmService.ConfirmEmail(token);
        if (!success)
        {
            return BadRequest("Invalid token or user ID does not match.");
        }

        if (user is null)
            throw new Exception("User not found after email confirmation.");

        // Login the user after email confirmation
        var tokens = await _authService.ForceLogin(user.Id);
        AddTokenCookies(tokens);
        return Ok();
    }

    [HttpPost("resendEmailConfirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ResendEmailConfirmation([FromQuery] string? lang)
    {
        var user = await _userService.GetCurrentUser();
        
        if (user.EmailConfirmed)
            return BadRequest("Email is already confirmed.");
        
        var token = await _emailConfirmService.GenerateToken(user.Id);
        await _emailScheduler.ScheduleVerificationEmail(user.Email, user.Name, token, lang ?? "en");
        return Ok("Email confirmation link has been resent.");
    }
}
