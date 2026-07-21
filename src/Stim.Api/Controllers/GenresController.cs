using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;

namespace Stim.Api.Controllers;

[Route("genres")]
[ApiController]
public class GenresController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenreDto>> GetGenres()
    {
        var genres = await context.Genres.AsNoTracking().Select(GenreQueries.ProjectToDto()).ToListAsync();

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
    public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto createGenreDto)
    {
        var genre = createGenreDto.ToEntity();

        await context.Genres.AddAsync(genre);

        await context.SaveChangesAsync();

        var result = genre.ToDto();

        return CreatedAtRoute("GetGenreBySlugOrId", new { genreSlugOrId = genre.Slug }, result);
    }
    [HttpPut("{genreId}")]
    public async Task<ActionResult> UpdateGenre(string genreId, [FromBody] UpdateGenreDto updateGenreDto)
    {
        var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == genreId);
        if (genre is null)
        {
            return NotFound();
        }

        genre.UpdateGenre(updateGenreDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
