using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Controllers;


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