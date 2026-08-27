using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stim.Api.Data;
using Stim.Api.Extensions;
using Stim.Api.Models.Authentication;
using Stim.Api.Models.User;
using Stim.Api.Services.Token;

namespace Stim.Api.Controllers;

[Route("authentication")]
[ApiController]
[AllowAnonymous]
public class AuthenticationController(UserManager<IdentityUser> userManager,
ApplicationIdentityDbContext identityDbContext,
ApplicationDbContext applicationDbContext,
TokenProvider tokenProvider) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser(RegisterUserDto registerUserDto)
    {
        using var transaction = await identityDbContext.Database.BeginTransactionAsync();
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        var identityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!identityResult.Succeeded)
        {
            identityResult.AddToModelState(ModelState);

            return ValidationProblem(ModelState);
        }

        var user = registerUserDto.ToEntity(identityUser.Id);

        await applicationDbContext.Users.AddAsync(user);

        await applicationDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        TokenRequest tokenRequest = new(identityUser.Id, identityUser.Email);

        var tokens = tokenProvider.Create(tokenRequest);

        return Ok(tokens);
    }
    [HttpPost("login")]
    public async Task<ActionResult<AccessTokenDto>> LoginUser(LoginUserDto loginUserDto, [FromServices] SignInManager<IdentityUser> signInManager)
    {
        var identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);
        if (identityUser is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            loginUserDto.Password,
            lockoutOnFailure: true
        );

        if (result.IsLockedOut)
        {
            var remainingTime = identityUser.LockoutEnd.HasValue ? identityUser.LockoutEnd.Value - DateTimeOffset.UtcNow : TimeSpan.Zero;

            return Problem(
              detail: $"Too Many Failed Attempts,Account is now locked out. Please try again in {Math.Ceiling(remainingTime.TotalMinutes)} minute(s).",
              statusCode: StatusCodes.Status423Locked
          );
        }

        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email);

        var tokens = tokenProvider.Create(tokenRequest);

        return Ok(tokens);
    }
}
