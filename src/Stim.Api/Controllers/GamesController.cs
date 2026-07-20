using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Game;

namespace Stim.Api.Controllers;

[Route("games")]
[ApiController]
public class GamesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GameCollectionDto>> GetGames()
    {
        var games = await context.Games.Select(GameQueries.ProjectToGameDto()).ToListAsync();

        var result = new GameCollectionDto()
        {
            Data = games
        };

        return Ok(result);
    }
    [HttpGet("{gameId}", Name = "GetGame")]
    public async Task<ActionResult<GameDto>> GetGame(string gameId)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }
        var result = game.ToDto();

        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<GameDto>> CreateGame([FromBody] CreateGameDto createGameDto)
    {
        if (!await context.Developers.AnyAsync(d => d.Id == createGameDto.DeveloperId))
        {
            return BadRequest(error: $"Game Developer With Id '{createGameDto.DeveloperId}' does not exist");
        }

        var game = createGameDto.ToEntity();

        await context.Games.AddAsync(game);

        await context.SaveChangesAsync();

        return CreatedAtRoute("GetGame", new { gameId = game.Id }, game.ToDto());
    }
    [HttpPut("{gameId}")]
    public async Task<ActionResult> UpdateGame(string gameId, [FromBody] UpdateGameDto updateGameDto)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            return NotFound();
        }
        if (!await context.Developers.AnyAsync(d => d.Id == updateGameDto.DeveloperId))
        {
            return BadRequest(error: $"Game Developer With Id '{updateGameDto.DeveloperId}' does not exist");
        }

        game.UpdateGame(updateGameDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPatch("{gameId}")]

    public async Task<ActionResult> PatchGame(string gameId, JsonPatchDocument<GameDto> document)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var gameDto = game.ToDto();

        document.ApplyTo(gameDto, ModelState);

        if (!TryValidateModel(gameDto))
        {
            return ValidationProblem(ModelState);
        }

        game.Title = gameDto.Title;

        game.Description = gameDto.Description;

        game.Price = gameDto.Price;

        game.LastUpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{gameId}")]
    public async Task<ActionResult> DeleteGame(string gameId)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        context.Games.Remove(game);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
