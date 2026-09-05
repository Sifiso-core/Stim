using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Filters;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Services.Concurrency;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Representation_Context;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("genres")]
[ApiController]
[ApiVersion(1.0)]
public class GenresController(ApplicationDbContext context, IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{

    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [HttpGet(Name = "GetGenres")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataCollectionResponse<GenreDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DataCollectionResponse<GenreDto>>> GetGenres(
    [FromQuery] GenreQueryParameters queries,
    SortMappingProvider sortMappingProvider,
    DataShapingService dataShapingService,
    CancellationToken cancellationToken)
    {
        var validationResult = ValidateQueryParameters(queries, sortMappingProvider, dataShapingService);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var genresQuery = BuildGenreQuery(queries, sortMappingProvider);

        var pageSize = queries.PageSize ?? GenreQueryParameters.GenreQueryDefaults.PageSize;

        DataCollectionResponse<GenreDto> dataCollectionResponse;

        if (queries.PaginationType == PaginationType.Cursor)
        {
            var result = await genresQuery.ToCursorPaginationResult(queries.Cursor, pageSize, cancellationToken);

            dataCollectionResponse = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };
        }
        else
        {
            var page = queries.Page ?? GenreQueryParameters.GenreQueryDefaults.Page;

            var result = await genresQuery.ToPaginationResultAsync(page, pageSize, cancellationToken);

            queries = queries with
            {
                Page = page
            };

            dataCollectionResponse = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };
        }

        AddHateoasLinks(dataCollectionResponse, queries);

        var shapedData = dataShapingService.ShapeCollectionData(dataCollectionResponse.Data, queries.Fields);

        var response = new DataCollectionResponse<ExpandoObject>
        {
            Data = [.. shapedData],
            Pagination = dataCollectionResponse.Pagination,
            Links = dataCollectionResponse.Links
        };

        return Ok(response);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{identifier}", Name = "GetGenreBySlugOrId")]
    [ETagConcurrencyFilterAttribute]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GenreDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<GenreDto>> GetGenreBySlugOrId(string identifier, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<GenreDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }
        var isId = identifier.StartsWith("g_", StringComparison.OrdinalIgnoreCase);

        var genre = await context.Genres.FirstOrDefaultAsync(g => isId ? g.Id == identifier : g.Slug == identifier.ToLower());

        if (genre is null)
        {
            return NotFound();
        }

        HttpContext.Items[HttpContextItemKeys.ResourceVersion] = genre.RowVersion;

        var genreDto = genre.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            genreDto.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, fields);
        }

        return Ok(genreDto);
    }
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{slug}/games", Name = "GetGamesByGenreSlug")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataCollectionResponse<GameDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetGamesByGenreSlug(string slug, GameQueryParameters queries,
    [FromServices] SortMappingProvider sortMappingProvider,
    [FromServices] DataShapingService dataShapingService,
    [FromServices] IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
    [FromServices] IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder)
    {
        if (!sortMappingProvider.ValidateMappings<GameDto, Game>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }
        if (!dataShapingService.Validate<GameDto>(queries.Fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {queries.Fields}");
        }

        var sortMappings = sortMappingProvider.GetMappings<GameDto, Game>();

        var normalisedString = slug.ToLower();

        var dataCollectionResponse = await context.Games.Where(game => game.Genres.Any(g => g.Slug == normalisedString))
                                                      .Select(GameQueries.ProjectToGameDto())
                                                      .ApplySort(queries.Sort, sortMappings).ToPaginationResultAsync(queries.Page ?? GenreQueryParameters.GenreQueryDefaults.Page, queries.PageSize ?? GenreQueryParameters.GenreQueryDefaults.PageSize);

        if (representationContext.IncludeHateoasLinks)
        {
            dataCollectionResponse.Links = gameLinkBuilder.CreateLinksForCollection(HttpContext, queries, dataCollectionResponse.Pagination.HasNextPage, dataCollectionResponse.Pagination.HasPreviousPage);
        }

        return Ok(dataCollectionResponse);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost(Name = "CreateGenre")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GenreDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto createGenreDto, [FromServices] IValidator<CreateGenreDto> validator)
    {

        await validator.ValidateAndThrowAsync(createGenreDto);

        var genre = createGenreDto.ToEntity();

        await context.Genres.AddAsync(genre);

        await context.SaveChangesAsync();

        var genreDto = genre.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            genreDto.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, null);
        }

        return CreatedAtRoute("GetGenreBySlugOrId", new { identifier = genre.Slug }, genreDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{genreId}", Name = "UpdateGenre")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> UpdateGenre(string genreId, [FromBody] UpdateGenreDto updateGenreDto, [FromServices] IValidator<UpdateGenreDto> validator)
    {

        await validator.ValidateAndThrowAsync(updateGenreDto);

        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == genreId);

        if (genre is null)
        {
            return NotFound();
        }
        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, genre, expectedVersion);

        genre.UpdateGenre(updateGenreDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{genreId}", Name = "DeleteGenre")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteGenre(string genreId)
    {
        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == genreId);

        if (genre is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, genre, expectedVersion);

        context.Genres.Remove(genre);

        await context.SaveChangesAsync();

        return NoContent();
    }
    private IQueryable<Genre> BuildGenreQuery(GenreQueryParameters queries, SortMappingProvider sortMappingProvider)
    {
        var search = queries.Search?.Trim().ToLower();

        var slug = queries.Slug?.Trim().ToLower();

        var sortMappings = sortMappingProvider.GetMappings<GenreDto, Genre>();

        return context.Genres.Where(genre => search == null || genre.Name.ToLower().Contains(search))
                            .Where(genre => slug == null || genre.Slug.ToLower() == slug)
                            .ApplySort(queries.Sort, sortMappings);
    }
    private ObjectResult? ValidateQueryParameters(
        GenreQueryParameters queries,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService)
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

        if (!sortMappingProvider.ValidateMappings<GenreDto, Genre>(queries.Sort))
        {
            return Problem($"The supplied sort parameter is invalid: '{queries.Sort}'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!dataShapingService.Validate<GenreDto>(queries.Fields))
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
    private void AddHateoasLinks(
        DataCollectionResponse<GenreDto> response,
        GenreQueryParameters queries)
    {
        if (!representationContext.IncludeHateoasLinks)
        {
            return;
        }

        foreach (var genre in response.Data)
        {
            genre.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genre.Id, queries.Fields);
        }

        if (response.Pagination?.PaginationType == PaginationType.Cursor)
        {
            response.Links = genreLinkBuilder.CreateCursorCollectionLinks(HttpContext, queries, response.Pagination.NextCursor, response.Pagination.PreviousCursor);

            return;
        }

        response.Links = genreLinkBuilder.CreateLinksForCollection(HttpContext, queries, response.Pagination?.HasNextPage ?? false, response.Pagination?.HasPreviousPage ?? false);
    }

}
