using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Common;
using Stim.Api.Models.User;
using Stim.Api.Services.User_Context;

namespace Stim.Api.Controllers;

[Authorize(Roles = Roles.Admin)]
[Route("users")]
[ApiController]
[ApiVersion(1.0)]
public class UsersController(ApplicationDbContext context, UserContext userContext) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        var user = await context.Users.Where(u => u.Id.Equals(userId)).Select(UserQueries.ProjectToDto()).FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);

    }
    [HttpGet("currentUser")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var userDto = await context.Users.Where(u => u.Id == userId).Select(UserQueries.ProjectToDto()).FirstOrDefaultAsync();

        return Ok(userDto);
    }
}
