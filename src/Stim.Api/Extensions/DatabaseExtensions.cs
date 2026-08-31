using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Common;

namespace Stim.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using ApplicationIdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            await identityDbContext.Database.MigrateAsync();

            app.Logger.LogInformation("Database migrations applied successfully.");

        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while applying database migrations.");
            throw;
        }
    }
    public static async Task SeedInitialDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] requiredRoles = [Roles.Member, Roles.Admin];
        try
        {
            foreach (var roleName in requiredRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create role '{Role}': {Errors}", roleName, errors);
                    }
                }
            }

            logger.LogInformation("Roles successfully seeded.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while seeding Identity roles.");
            throw;
        }
    }
}

