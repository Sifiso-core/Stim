using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("genres")]
[ApiController]
public class GenresController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenreDto>> GetGenres([FromQuery] GenreQueryParameters queries, SortMappingProvider sortMappingProvider)
    {
        if (!sortMappingProvider.ValidateMappings<GenreDto, Genre>(queries.Sort))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided sort parameters is invalid '{queries.Sort}'");
        }
        var sortMappings = sortMappingProvider.GetMappings<GenreDto, Genre>();

        var search = queries.Search?.Trim().ToLower();

        var slug = queries.Slug?.Trim().ToLower();

        var genres = await context.Genres
            .Where(g => search == null || g.Name.ToLower().Contains(search))
            .Where(g => slug == null || g.Slug.ToLower().Equals(slug))
            .ApplySort(queries.Sort, sortMappings)
            .Select(GenreQueries.ProjectToDto()).ToListAsync();

        var result = new DataCollectionResponse<GenreDto>()
        {
            Data = genres
        };

        return Ok(result);
    }
    [HttpGet("{identifier}", Name = "GetGenreBySlugOrId")]
    public async Task<ActionResult<GenreDto>> GetGenreBySlugOrId(string identifier)
    {
        var isId = identifier.StartsWith("g_", StringComparison.OrdinalIgnoreCase);

        var genre = await context.Genres.FirstOrDefaultAsync(g => isId ? g.Id == identifier : g.Slug == identifier.ToLower());

        var result = genre?.ToDto();

        return Ok(result);
    }
    [HttpGet("{slug}/games")]
    public async Task<ActionResult<DataCollectionResponse<GameDto>>> GetGamesByGenreSlug(string slug)
    {
        var normalisedString = slug.ToLower();

        var games = await context.Games.Where(game => game.Genres.Any(g => g.Slug == normalisedString))
                                                      .Select(GameQueries.ProjectToGameDto())
                                                      .ToListAsync();

        var result = new DataCollectionResponse<GameDto>
        {
            Data = games
        };

        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto createGenreDto, [FromServices] IValidator<CreateGenreDto> validator)
    {

        await validator.ValidateAndThrowAsync(createGenreDto);

        var genre = createGenreDto.ToEntity();

        await context.Genres.AddAsync(genre);

        await context.SaveChangesAsync();

        var result = genre.ToDto();

        return CreatedAtRoute("GetGenreBySlugOrId", new { identifier = genre.Slug }, result);
    }
    [HttpPut("{genreId}")]
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
    [HttpDelete("{genreId}")]
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
