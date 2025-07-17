using Api.Controllers.Base;
using Api.Services;
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

    public UserController(IUserService userService, IEmailConfirmService emailConfirmService, IAuthService authService,
        IAppDomainService appDomainService, AppSettings appSettings) : base(appDomainService, appSettings)
    {
        _userService = userService;
        _emailConfirmService = emailConfirmService;
        _authService = authService;
    }

    [HttpPost("confirmEmail/{token}")]
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
}
