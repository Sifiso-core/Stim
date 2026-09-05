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
    public async Task<ActionResult<DataCollectionResponse<DeveloperDto>>> GetDevelopers(
    [FromQuery] DeveloperQueryParameters queries,
    SortMappingProvider sortMappingProvider,
    DataShapingService dataShapingService,
    [FromServices]
    IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    [FromServices]
    IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
    [FromServices]
    IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder,
    CancellationToken cancellationToken)
    {
        var validationResult = ValidateQueryParameters(queries, sortMappingProvider, dataShapingService);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var developersQuery = BuildDeveloperQuery(queries, sortMappingProvider);

        var pageSize = queries.PageSize ?? DeveloperQueryParameters.DeveloperQueryDefaults.PageSize;

        DataCollectionResponse<DeveloperDto> paginationResult;

        if (queries.PaginationType == PaginationType.Cursor)
        {
            var result = await developersQuery.ToCursorPaginationResult(queries.Cursor, pageSize, cancellationToken);

            paginationResult = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };

        }
        else
        {
            var page = queries.Page ?? DeveloperQueryParameters.DeveloperQueryDefaults.Page;

            var result = await developersQuery.ToPaginationResultAsync(page, pageSize, cancellationToken);

            queries = queries with
            {
                Page = page
            };

            paginationResult = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };

        }

        AddHateoasLinks(paginationResult, queries, gameLinkBuilder, tagLinkBuilder, genreLinkBuilder);

        var shapedData = dataShapingService.ShapeCollectionData(
            paginationResult.Data,
            queries.Fields);

        var response = new DataCollectionResponse<ExpandoObject>
        {
            Data = [.. shapedData],
            Pagination = paginationResult.Pagination,
            Links = paginationResult.Links
        };

        return Ok(response);
    }

    [HttpGet("{developerId}", Name = "GetDeveloper")]
    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [ETagConcurrencyFilterAttribute]
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
    private ObjectResult? ValidateQueryParameters(DeveloperQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService)
    {
        if (queries.Page is not null && queries.Cursor is not null)
        {
            return Problem("The 'page' and 'cursor' query parameters cannot be used together.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.Page is not null && queries.Page < 1)
        {
            return Problem("The 'page' query parameter must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PageSize is not null && queries.PageSize < 1)
        {
            return Problem("The 'pageSize' query parameter must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!sortMappingProvider.ValidateMappings<DeveloperDto, Developer>(queries.Sort))
        {
            return Problem($"The supplied sort parameter is invalid: '{queries.Sort}'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!dataShapingService.Validate<DeveloperDto>(queries.Fields))
        {
            return Problem($"The supplied fields parameter is invalid: '{queries.Fields}'.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (queries.PaginationType == PaginationType.Cursor && !string.IsNullOrWhiteSpace(queries.Sort))
        {
            return Problem("Custom sorting is not supported with cursor pagination. " + "Cursor pagination uses CreatedAtUtc descending and Id ascending.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (queries.PaginationType == PaginationType.Cursor && queries.Page is not null)
        {
            return Problem("'page' cannot be used with cursor pagination.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (queries.PaginationType == PaginationType.Offset && !string.IsNullOrWhiteSpace(queries.Cursor))
        {
            return Problem("'Cursor' cannot be used with offset pagination. Review your parameters", statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
    private IQueryable<Developer> BuildDeveloperQuery(
    DeveloperQueryParameters queries,
    SortMappingProvider sortMappingProvider)
    {
        var search = queries.Search?.Trim().ToLower();

        var sortMappings = sortMappingProvider.GetMappings<DeveloperDto, Developer>();

        return context.Developers.Include(d => d.Games).Where(developer => search == null || developer.Name.ToLower().Contains(search)).ApplySort(queries.Sort, sortMappings);
    }
    private void AddHateoasLinks(
    DataCollectionResponse<DeveloperDto> response,
    DeveloperQueryParameters queries,
    IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
    IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder)
    {
        if (!representationContext.IncludeHateoasLinks)
        {
            return;
        }

        foreach (var developer in response.Data)
        {
            AddDeveloperLinks(developer, queries, gameLinkBuilder, tagLinkBuilder, genreLinkBuilder);
        }

        if (response.Pagination?.PaginationType == PaginationType.Cursor)
        {
            response.Links =
                developerLinkBuilder.CreateCursorCollectionLinks(HttpContext, queries, response.Pagination.NextCursor, response.Pagination.PreviousCursor);

            return;
        }

        response.Links =
            developerLinkBuilder.CreateLinksForCollection(HttpContext, queries, response.Pagination?.HasNextPage ?? false, response.Pagination?.HasPreviousPage ?? false);
    }
    private void AddDeveloperLinks(
    DeveloperDto developer,
    DeveloperQueryParameters queries,
    IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
    IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder)
    {
        foreach (var game in developer.Games)
        {
            game.Links = gameLinkBuilder.CreateLinksForResource(HttpContext, game.Id, queries.Fields);

            foreach (var tag in game.Tags)
            {
                tag.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, tag.Id, queries.Fields);
            }

            foreach (var genre in game.Genres)
            {
                genre.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genre.Id, queries.Fields);
            }
        }

        developer.Links = developerLinkBuilder.CreateLinksForResource(HttpContext, developer.Id, queries.Fields);
    }

}
