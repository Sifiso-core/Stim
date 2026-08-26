using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Developer;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("developers")]
[ApiController]
[ApiVersion(1.0)]
public class DevelopersController(ApplicationDbContext context, IHateoasLinkBuilder<DeveloperDto, DeveloperQueryParameters> developerLinkBuilder) : ControllerBase
{
    private bool IncludeHateoasLinks => Request.Headers.Accept.Contains(CustomMediaTypeNames.Application.HateoasJsonMediaType);
    [HttpGet(Name = "GetDevelopers")]
    public async Task<IActionResult> GetDevelopers([FromQuery] DeveloperQueryParameters queries,
     SortMappingProvider sortMappingProvider,
     DataShapingService dataShapingService,
    [FromServices] IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    [FromServices] IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
    [FromServices] IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder)
    {
        if (!sortMappingProvider.ValidateMappings<DeveloperDto, Developer>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }
        if (!dataShapingService.Validate<DeveloperDto>(queries.Fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {queries.Fields}");
        }

        var sortMappings = sortMappingProvider.GetMappings<DeveloperDto, Developer>();

        var search = queries.Search?.Trim().ToLower();

        var developersQueryable = context.Developers.Where(d => search == null || d.Name.ToLower().Contains(search))
                                                                    .ApplySort(queries.Sort, sortMappings)
                                                                    .Select(DeveloperQueries.ProjectToDto());


        var paginationResult = await developersQueryable.ToPaginationResultAsync(queries.Page, queries.PageSize);

        List<LinkDto> links = [];

        if (IncludeHateoasLinks)
        {
            links.AddRange(developerLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));
            paginationResult.Links = links;
        }

        var result = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, IncludeHateoasLinks ? d =>
        {
            foreach (var game in d.Games)
            {
                game.Links = gameLinkBuilder.CreateLinksForResource(HttpContext, game.Id, queries.Fields);

                game.Tags.ForEach(t => t.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, t.Id, queries.Fields));

                game.Genres.ForEach(g => g.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, g.Id, queries.Fields));
            }

            return developerLinkBuilder.CreateLinksForResource(HttpContext, d.Id, queries.Fields);
        }
            : null),

            Links = IncludeHateoasLinks ? links : null

        };

        return Ok(result);
    }
    [HttpGet("{developerId}", Name = "GetDeveloper")]
    public async Task<IActionResult> GetDeveloper(string developerId, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<DeveloperDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }

        var developer = await context.Developers.Include(d => d.Games).Select(DeveloperQueries.ProjectToDto()).FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }
        var result = dataShapingService.ShapeData(developer, fields);

        if (IncludeHateoasLinks)
        {
            result.TryAdd("links", developerLinkBuilder.CreateLinksForResource(HttpContext, developerId, fields));

        }

        return Ok(result);
    }

    [HttpPost(Name = "CreateDeveloper")]
    public async Task<ActionResult<DeveloperDto>> CreateDeveloper([FromBody] CreateDeveloperDto createDeveloperDto, [FromServices] IValidator<CreateDeveloperDto> validator)
    {
        await validator.ValidateAndThrowAsync(createDeveloperDto);

        var developer = createDeveloperDto.ToEntity();

        await context.Developers.AddAsync(developer);

        await context.SaveChangesAsync();

        var developerDto = developer.ToDto();

        if (IncludeHateoasLinks)
        {
            developerDto.Links = developerLinkBuilder.CreateLinksForResource(HttpContext, developerDto.Id, null);
        }


        return CreatedAtRoute("GetDeveloper", new { developerId = developer.Id }, developerDto);
    }
    [HttpPut("{developerId}", Name = "UpdateDeveloper")]
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
    [HttpPatch("{developerId}", Name = "PatchDeveloper")]
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
    [HttpDelete("{developerId}", Name = "DeleteDeveloper")]
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
