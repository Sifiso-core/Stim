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
using Stim.Api.Services.User_Context;

namespace Stim.Api.Controllers;

[Route("genres")]
[ApiController]
[ApiVersion(1.0)]
public class GenresController(ApplicationDbContext context, IHateoasLinkBuilder<GenreDto, GenreQueryParameters> hateoasLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{

    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [HttpGet(Name = "GetGenres")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataCollectionResponse<GenreDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DataCollectionResponse<GenreDto>>> GetGenres([FromQuery] GenreQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService)
    {
        if (!sortMappingProvider.ValidateMappings<GenreDto, Genre>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }
        if (!dataShapingService.Validate<GenreDto>(queries.Fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {queries.Fields}");
        }
        var sortMappings = sortMappingProvider.GetMappings<GenreDto, Genre>();

        var search = queries.Search?.Trim().ToLower();

        var slug = queries.Slug?.Trim().ToLower();

        var genresQueryable = context.Genres
            .Where(g => search == null || g.Name.ToLower().Contains(search))
            .Where(g => slug == null || g.Slug.ToLower().Equals(slug))
            .ApplySort(queries.Sort, sortMappings)
            .Select(GenreQueries.ProjectToDto());

        var paginationResult = await genresQueryable.ToPaginationResultAsync(queries.Page, queries.PageSize);

        var links = new List<LinkDto>();

        if (representationContext.IncludeHateoasLinks)
        {
            links.AddRange(hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));

            paginationResult.Links = links;

        }

        var result = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, representationContext.IncludeHateoasLinks ? g => hateoasLinkBuilder.CreateLinksForResource(HttpContext, g.Id, queries.Fields) : null),

            Links = representationContext.IncludeHateoasLinks ? links : null
        };

        return Ok(result);
    }
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{identifier}", Name = "GetGenreBySlugOrId")]
    [ETagCache]
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
            genreDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, fields);
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

        var paginationResult = await context.Games.Where(game => game.Genres.Any(g => g.Slug == normalisedString))
                                                      .Select(GameQueries.ProjectToGameDto())
                                                      .ApplySort(queries.Sort, sortMappings).ToPaginationResultAsync(queries.Page, queries.PageSize);

        var links = new List<LinkDto>();

        if (representationContext.IncludeHateoasLinks)
        {
            links.AddRange(gameLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));
        }

        var result = new DataCollectionResponse<ExpandoObject>
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, representationContext.IncludeHateoasLinks ? d =>
            {
                foreach (var genre in d.Genres)
                {
                    genre.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genre.Id, queries.Fields);
                }
                return gameLinkBuilder.CreateLinksForResource(HttpContext, d.Id, queries.Fields);
            }
            : null),

            Links = representationContext.IncludeHateoasLinks ? links : null
        };

        return Ok(result);
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
            genreDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, null);
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
}
