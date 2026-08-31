using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Extensions;
using Stim.Api.Models.Authentication;
using Stim.Api.Models.Common;
using Stim.Api.Models.User;
using Stim.Api.Options;
using Stim.Api.Services.Token;

namespace Stim.Api.Controllers;

[Route("authentication")]
[ApiController]
[AllowAnonymous]
public class AuthenticationController(UserManager<IdentityUser> userManager,
ApplicationIdentityDbContext identityDbContext,
ApplicationDbContext applicationDbContext,
TokenProvider tokenProvider,
IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly JwtAuthOptions options = options.Value;

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessTokenDto))]
    public async Task<ActionResult<AccessTokenDto>> RegisterUser(RegisterUserDto registerUserDto)
    {
        using var transaction = await identityDbContext.Database.BeginTransactionAsync();
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        var createUserResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!createUserResult.Succeeded)
        {
            createUserResult.AddToModelState(ModelState);

            return ValidationProblem(ModelState);
        }

        var addToRoleResult = await userManager.AddToRoleAsync(identityUser, Roles.Member);

        if (!addToRoleResult.Succeeded)
        {
            addToRoleResult.AddToModelState(ModelState);

            return Problem("Unable To Assign Role To User");
        }

        var user = registerUserDto.ToEntity(identityUser.Id);

        await applicationDbContext.Users.AddAsync(user);

        await applicationDbContext.SaveChangesAsync();

        TokenRequest tokenRequest = new(identityUser.Id, identityUser.Email, [Roles.Member]);

        var tokens = tokenProvider.Create(tokenRequest);


        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = tokens.AccessToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(options.RefreshTokenExpirationDays)
        };

        await identityDbContext.RefreshTokens.AddAsync(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(tokens);
    }
    [HttpPost("register/admin")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessTokenDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<ActionResult<AccessTokenDto>> RegisterAdminUser(RegisterUserDto registerUserDto)
    {
        using var transaction = await identityDbContext.Database.BeginTransactionAsync();
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        var createUserResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!createUserResult.Succeeded)
        {
            createUserResult.AddToModelState(ModelState);

            return ValidationProblem(ModelState);
        }

        var addToRoleResult = await userManager.AddToRoleAsync(identityUser, Roles.Admin);

        if (!addToRoleResult.Succeeded)
        {
            addToRoleResult.AddToModelState(ModelState);

            return Problem("Unable To Assign Role To User");
        }

        var user = registerUserDto.ToEntity(identityUser.Id);

        await applicationDbContext.Users.AddAsync(user);

        await applicationDbContext.SaveChangesAsync();

        TokenRequest tokenRequest = new(identityUser.Id, identityUser.Email, [Roles.Admin]);

        var tokens = tokenProvider.Create(tokenRequest);


        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = tokens.AccessToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(options.RefreshTokenExpirationDays)
        };

        await identityDbContext.RefreshTokens.AddAsync(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(tokens);
    }
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessTokenDto))]
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

        var roles = await userManager.GetRolesAsync(identityUser);

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!, roles);

        var tokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = tokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(options.RefreshTokenExpirationDays)
        };

        await identityDbContext.RefreshTokens.AddAsync(refreshToken);

        await identityDbContext.SaveChangesAsync();

        return Ok(tokens);
    }
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessTokenDto))]
    public async Task<ActionResult<AccessTokenDto>> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var refreshToken = await identityDbContext.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(refreshToken.User);

        var tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.Email!, roles);

        var tokens = tokenProvider.Create(tokenRequest);

        refreshToken.Token = tokens.RefreshToken;

        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(options.RefreshTokenExpirationDays);

        await identityDbContext.SaveChangesAsync();

        return Ok(tokens);

    }

}