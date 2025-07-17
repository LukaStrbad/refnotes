using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEmailConfirmService _emailConfirmService;

    public UserController(IUserService userService, IEmailConfirmService emailConfirmService)
    {
        _userService = userService;
        _emailConfirmService = emailConfirmService;
    }

    [HttpPost("confirmEmail/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmEmail(string token)
    {
        var currentUser = await _userService.GetCurrentUser();
        
        var result = await _emailConfirmService.ConfirmEmail(token, currentUser.Id);
        if (!result)
        {
            return BadRequest("Invalid token or user ID does not match.");
        }

        return Ok();
    }
}
