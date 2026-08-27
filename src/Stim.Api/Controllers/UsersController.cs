using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.User;

namespace Stim.Api.Controllers;

[Route("users")]
[ApiController]
public class UsersController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(string userId)
    {
        var user = await context.Users.Where(u => u.Id.Equals(userId)).Select(UserQueries.ProjectToDto()).FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);

    }
}
