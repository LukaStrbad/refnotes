using Api.Controllers.Base;
using Api.Exceptions;
using Api.Model;
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
    private readonly IPasswordResetService _passwordResetService;

    public UserController(IUserService userService, IEmailConfirmService emailConfirmService, IAuthService authService,
        IAppDomainService appDomainService, AppSettings appSettings, IEmailScheduler emailScheduler,
        IPasswordResetService passwordResetService) : base(
        appDomainService, appSettings)
    {
        _userService = userService;
        _emailConfirmService = emailConfirmService;
        _authService = authService;
        _emailScheduler = emailScheduler;
        _passwordResetService = passwordResetService;
    }

    [HttpGet("accountInfo")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult> AccountInfo()
    {
        var user = await _userService.GetCurrentUser();
        return Ok(UserResponse.FromUser(user));
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

    [HttpPost("updatePasswordByToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorCodeResponse>(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult> UpdatePasswordByToken([FromBody] UpdatePasswordByTokenRequest request)
    {
        try
        {
            var user = await _userService.GetByUsername(request.Username);
            if (!user.EmailConfirmed)
                return BadRequest(ErrorCodes.EmailNotConfirmed);
            if (!await _passwordResetService.ValidateToken(user.Id, request.Token))
                return BadRequest(ErrorCodes.InvalidOrExpiredToken);

            await _userService.UpdatePassword(new UserCredentials(request.Username, request.Password));
            await _passwordResetService.DeleteTokensForUser(user.Id);
            return Ok();
        }
        catch (UserNotFoundException)
        {
            return BadRequest(ErrorCodes.UserNotFound);
        }
    }

    [HttpPost("sendPasswordResetEmail")]
    [AllowAnonymous]
    public async Task<ActionResult> SendPasswordResetEmail([FromQuery] string username, [FromQuery] string? lang)
    {
        try
        {
            var user = await _userService.GetByUsername(username);

            if (!user.EmailConfirmed)
                return BadRequest("Email is not confirmed. Cannot send password reset email.");

            var token = await _passwordResetService.GenerateToken(user.Id);
            await _emailScheduler.SchedulePasswordResetEmail(user, token, lang ?? "en");

            return Ok();
        }
        catch (UserNotFoundException)
        {
            return BadRequest("User not found.");
        }
    }

    [HttpPost("edit")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult> EditUser([FromBody] EditUserRequest request, [FromQuery] string? lang)
    {
        var currentUser = await _userService.GetCurrentUser();

        var emailsDifferent = currentUser.Email != request.NewEmail;

        try
        {
            var updatedUser = await _userService.EditUser(currentUser.Id, request);

            if (emailsDifferent)
            {
                await _userService.UnconfirmEmail(updatedUser.Id);
                await _emailConfirmService.DeleteTokensForUser(updatedUser.Id);
                await _passwordResetService.DeleteTokensForUser(updatedUser.Id);
                var token = await _emailConfirmService.GenerateToken(updatedUser.Id);
                await _emailScheduler.ScheduleVerificationEmail(updatedUser.Email, updatedUser.Name, token,
                    lang ?? "en");
            }

            // Login the user after email confirmation
            var tokens = await _authService.ForceLogin(updatedUser.Id);
            AddTokenCookies(tokens);
            return Ok(UserResponse.FromUser(updatedUser));
        }
        catch (UserExistsException)
        {
            return BadRequest("Username already exists.");
        }
    }

    [HttpPost("updatePassword")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var currentUser = await _userService.GetCurrentUser();

        // Validate the old password
        if (!await _authService.VerifyPassword(new UserCredentials(currentUser.Username, request.OldPassword)))
        {
            return BadRequest("Invalid old password.");
        }

        // Update the password
        await _userService.UpdatePassword(new UserCredentials(currentUser.Username, request.NewPassword));
        return Ok("Password updated successfully.");
    }
}
