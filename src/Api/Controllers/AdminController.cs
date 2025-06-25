using Api.Exceptions;
using Api.Model;
using Api.Services;
using Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;


[ApiController]
[Route("[controller]")]
[Authorize("admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpPost("modifyRoles")]
    [ProducesResponseType<Ok<List<string>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<NotFound>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> ModifyRoles(ModifyRolesRequest modifyRolesRequest)
    {
        try
        {
            var roles = await adminService.ModifyRoles(modifyRolesRequest);
            return Ok(roles);
        } catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpGet("listUsers")]
    [ProducesResponseType<Ok<List<User>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<User>>> ListUsers()
    {
        var users = await adminService.ListUsers();
        return Ok(users);
    }
}