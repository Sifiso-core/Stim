using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Filters;
using Stim.Api.Models.Common;
using Stim.Api.Models.Developer;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Concurrency;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Representation_Context;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("developers")]
[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[ResponseCache(Duration = 120)]
public class DevelopersController(ApplicationDbContext context, IHateoasLinkBuilder<DeveloperDto, DeveloperQueryParameters> developerLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{

    [HttpGet(Name = "GetDevelopers")]
    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataCollectionResponse<DeveloperDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails), Description = "BadRequest: Ambiguous Api Version, Use One Versioned Media Type In Your 'Accept Header'")]
    public async Task<ActionResult<DataCollectionResponse<DeveloperDto>>> GetDevelopers([FromQuery] DeveloperQueryParameters queries,
     SortMappingProvider sortMappingProvider,
     DataShapingService dataShapingService,
    [FromServices] IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    [FromServices] IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
    [FromServices] IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder
  )
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

        if (representationContext.IncludeHateoasLinks)
        {
            links.AddRange(developerLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));
            paginationResult.Links = links;
        }

        var response = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, representationContext.IncludeHateoasLinks ? d =>
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

            Links = representationContext.IncludeHateoasLinks ? links : null

        };

        return Ok(response);
    }
    [HttpGet("{developerId}", Name = "GetDeveloper")]
    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [ETagCache]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeveloperDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetDeveloper(string developerId, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<DeveloperDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }

        var developer = await context.Developers.Include(d => d.Games).FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        HttpContext.Items[HttpContextItemKeys.ResourceVersion] = developer.RowVersion;

        var response = dataShapingService.ShapeData(developer.ToDto(), fields);

        if (representationContext.IncludeHateoasLinks)
        {
            response.TryAdd("links", developerLinkBuilder.CreateLinksForResource(HttpContext, developerId, fields));
        }

        return Ok(response);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost(Name = "CreateDeveloper")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DeveloperDto))]
    public async Task<ActionResult<DeveloperDto>> CreateDeveloper([FromBody] CreateDeveloperDto createDeveloperDto, [FromServices] IValidator<CreateDeveloperDto> validator)
    {
        await validator.ValidateAndThrowAsync(createDeveloperDto);

        var developer = createDeveloperDto.ToEntity();

        await context.Developers.AddAsync(developer);

        await context.SaveChangesAsync();

        var developerDto = developer.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            developerDto.Links = developerLinkBuilder.CreateLinksForResource(HttpContext, developerDto.Id, null);
        }

        return CreatedAtRoute("GetDeveloper", new { developerId = developer.Id }, developerDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{developerId}", Name = "UpdateDeveloper")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateDeveloper(string developerId, [FromBody] UpdateDeveloperDto updateDeveloperDto, [FromServices] IValidator<UpdateDeveloperDto> validator)
    {
        await validator.ValidateAndThrowAsync(updateDeveloperDto);

        var developer = await context.Developers.FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, developer, expectedVersion);

        developer.UpdateDeveloper(updateDeveloperDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{developerId}", Name = "PatchDeveloper")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
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

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, developer, expectedVersion);

        developer.UpdateDeveloper(developerDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{developerId}", Name = "DeleteDeveloper")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteDeveloper(string developerId)
    {
        var developer = await context.Developers.FirstOrDefaultAsync(d => d.Id == developerId);

        if (developer is null)
        {
            return NotFound();
        }
        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, developer, expectedVersion);

        context.Developers.Remove(developer);

        await context.SaveChangesAsync();

        return NoContent();

    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("batch", Name = "CreateBatchDevelopers")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DeveloperDto>))]
    public async Task<ActionResult<IEnumerable<DeveloperDto>>> CreateBatchDevelopers(
    [FromBody] List<CreateDeveloperDto> createDeveloperDtos,
    [FromServices] IValidator<CreateDeveloperDto> validator)
    {
        if (createDeveloperDtos is null || createDeveloperDtos.Count == 0)
        {
            return BadRequest("Developer list cannot be empty.");
        }

        foreach (var dto in createDeveloperDtos)
        {
            await validator.ValidateAndThrowAsync(dto);
        }

        var developers = createDeveloperDtos.Select(dto => dto.ToEntity()).ToList();

        await context.Developers.AddRangeAsync(developers);

        await context.SaveChangesAsync();


        var developerDtos = developers.Select(developer =>
        {
            var dto = developer.ToDto();
            if (representationContext.IncludeHateoasLinks)
            {
                dto.Links = developerLinkBuilder.CreateLinksForResource(HttpContext, dto.Id, null);
            }

            return dto;

        }).ToList();

        return Ok(developerDtos);
    }
}
