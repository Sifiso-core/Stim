using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Developer;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("developers")]
[ApiController]
public class DevelopersController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DataCollectionResponse<DeveloperDto>>> GetDevelopers([FromQuery] DeveloperQueryParameters queries, SortMappingProvider sortMappingProvider)
    {
        if (!sortMappingProvider.ValidateMappings<DeveloperDto, Developer>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }

        var sortMappings = sortMappingProvider.GetMappings<DeveloperDto, Developer>();

        var search = queries.Search?.Trim().ToLower();

        var developers = await context.Developers.Where(d => search == null || d.Name.ToLower().Contains(search))
                                                                    .ApplySort(queries.Sort, sortMappings)
                                                                    .Select(DeveloperQueries.ProjectToDto())
                                                                    .ToListAsync();

        var result = new DataCollectionResponse<DeveloperDto>
        {
            Data = developers
        };

        return Ok(result);
    }
    [HttpGet("{developerId}", Name = "GetDeveloper")]
    public async Task<ActionResult<DeveloperDto>> GetDeveloper(string developerId)
    {
        var developer = await context.Developers.Include(d => d.Games).Select(DeveloperQueries.ProjectToDto()).FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        return Ok(developer);
    }
    [HttpPost]
    public async Task<ActionResult<DeveloperDto>> CreateDeveloper([FromBody] CreateDeveloperDto createDeveloperDto, [FromServices] IValidator<CreateDeveloperDto> validator)
    {
        await validator.ValidateAndThrowAsync(createDeveloperDto);

        var developer = createDeveloperDto.ToEntity();

        await context.Developers.AddAsync(developer);

        await context.SaveChangesAsync();

        var result = developer.ToDto();

        return CreatedAtRoute("GetDeveloper", new { developerId = developer.Id }, result);
    }
    [HttpPut("{developerId}")]
    public async Task<ActionResult> UpdateDeveloper(string developerId, [FromBody] UpdateDeveloperDto updateDeveloperDto, [FromServices] IValidator<UpdateDeveloperDto> validator)
    {
        await validator.ValidateAndThrowAsync(updateDeveloperDto);

        var developer = await context.Developers.FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        developer.UpdateDeveloper(updateDeveloperDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPatch("{developerId}")]
    public async Task<ActionResult> PatchDeveloper(string developerId, JsonPatchDocument<DeveloperDto> document)
    {
        var developer = await context.Developers.FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        var developerDto = developer.ToDto();

        document.ApplyTo(developerDto, ModelState);

        if (!TryValidateModel(ModelState))
        {
            return ValidationProblem(ModelState);
        }

        developer.UpdateDeveloper(developerDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("{developerId}")]
    public async Task<ActionResult> DeleteDeveloper(string developerId)
    {
        var developer = await context.Developers.FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        context.Developers.Remove(developer);

        await context.SaveChangesAsync();

        return NoContent();

    }

}
