using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Services;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("genres")]
[ApiController]
[ApiVersion(1.0)]
public class GenresController(ApplicationDbContext context, IHateoasLinkBuilder<GenreDto, GenreQueryParameters> hateoasLinkBuilder) : ControllerBase
{
    private bool IncludeHateoasLinks => Request.Headers.Accept.Contains(CustomMediaTypeNames.Application.HateoasJsonMediaType);
    [HttpGet(Name = "GetGenres")]
    public async Task<ActionResult<GenreDto>> GetGenres([FromQuery] GenreQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService)
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

        if (IncludeHateoasLinks)
        {
            links.AddRange(hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));

            paginationResult.Links = links;

        }

        var result = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, IncludeHateoasLinks ? g => hateoasLinkBuilder.CreateLinksForResource(HttpContext, g.Id, queries.Fields) : null),

            Links = IncludeHateoasLinks ? links : null
        };

        return Ok(result);
    }
    [HttpGet("{identifier}", Name = "GetGenreBySlugOrId")]
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

        var genreDto = genre.ToDto();

        if (IncludeHateoasLinks)
        {
            genreDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, fields);
        }

        return Ok(genreDto);
    }
    [HttpGet("{slug}/games", Name = "GetGamesByGenreSlug")]
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

        if (IncludeHateoasLinks)
        {
            links.AddRange(gameLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));
        }

        var result = new DataCollectionResponse<ExpandoObject>
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, IncludeHateoasLinks ? d =>
            {
                foreach (var genre in d.Genres)
                {
                    genre.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genre.Id, queries.Fields);
                }
                return gameLinkBuilder.CreateLinksForResource(HttpContext, d.Id, queries.Fields);
            }
            : null),

            Links = IncludeHateoasLinks ? links : null
        };

        return Ok(result);
    }
    [HttpPost(Name = "CreateGenre")]
    public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto createGenreDto, [FromServices] IValidator<CreateGenreDto> validator)
    {

        await validator.ValidateAndThrowAsync(createGenreDto);

        var genre = createGenreDto.ToEntity();

        await context.Genres.AddAsync(genre);

        await context.SaveChangesAsync();

        var genreDto = genre.ToDto();

        if (IncludeHateoasLinks)
        {
            genreDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, genreDto.Id, null);
        }

        return CreatedAtRoute("GetGenreBySlugOrId", new { identifier = genre.Slug }, genreDto);
    }
    [HttpPut("{genreId}", Name = "UpdateGenre")]
    public async Task<ActionResult> UpdateGenre(string genreId, [FromBody] UpdateGenreDto updateGenreDto, [FromServices] IValidator<UpdateGenreDto> validator)
    {

        await validator.ValidateAndThrowAsync(updateGenreDto);

        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == genreId);

        if (genre is null)
        {
            return NotFound();
        }

        genre.UpdateGenre(updateGenreDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("{genreId}", Name = "DeleteGenre")]
    public async Task<ActionResult> DeleteGenre(string genreId)
    {
        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == genreId);
        if (genre is null)
        {
            return NotFound();
        }
        context.Genres.Remove(genre);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
