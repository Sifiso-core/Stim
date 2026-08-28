using System.Dynamic;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;
using Stim.Api.Models.GameTag;
using Stim.Api.Models.Genre;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;

[Route("games")]
[ApiController]
[ApiVersion(1.0)]
public class GamesController(ApplicationDbContext context, IHateoasLinkBuilder<GameDto, GameQueryParameters> hateoasLinkBuilder) : ControllerBase
{
    private bool IncludeHateoasLinks => Request.Headers.Accept.Contains(CustomMediaTypeNames.Application.HateoasJsonMediaType);

    [HttpGet(Name = "GetGames")]
    public async Task<ActionResult<DataCollectionResponse<GameDto>>> GetGames([FromQuery] GameQueryParameters queries, SortMappingProvider sortMappingProvider, DataShapingService dataShapingService, IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder)
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

        var search = queries.Search?.Trim().ToLower();

        var gamesQueryable = context.Games.Include(g => g.Tags)
        .Where(g => search == null || g.Title.ToLower().Contains(search) || g.Description != null && g.Description.ToLower().Contains(search))
        .ApplySort(queries.Sort, sortMappings)
        .Select(GameQueries.ProjectToGameDto());

        var paginationResult = await gamesQueryable.ToPaginationResultAsync(queries.Page, queries.PageSize);

        var links = new List<LinkDto>();

        if (IncludeHateoasLinks)
        {
            links.AddRange(hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage));

            paginationResult.Links = links;

        }

        var result = new DataCollectionResponse<ExpandoObject>()
        {
            Data = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields, IncludeHateoasLinks ? d =>
            {
                d.Genres.ForEach(g => g.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, g.Id, queries.Fields));
                return hateoasLinkBuilder.CreateLinksForResource(HttpContext, d.Id, queries.Fields);
            }
            : null),
            Links = IncludeHateoasLinks ? links : null
        };

        return Ok(result);
    }
    [HttpGet("{gameId}", Name = "GetGame")]
    public async Task<ActionResult<GameDto>> GetGame(string gameId, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<GameDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }
        var game = await context.Games.Include(g => g.GameTags).Include(g => g.Genres).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }
        var gameDto = game.ToDto();

        if (IncludeHateoasLinks)
        {
            gameDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, gameDto.Id, fields);

        }

        return Ok(gameDto);
    }
    [HttpPost(Name = "CreateGame")]
    public async Task<ActionResult<GameDto>> CreateGame([FromBody] CreateGameDto createGameDto, [FromServices] IValidator<CreateGameDto> validator)
    {
        await validator.ValidateAndThrowAsync(createGameDto);

        if (!await context.Developers.AnyAsync(d => d.Id == createGameDto.DeveloperId))
        {
            return BadRequest(error: $"Game Developer With Id '{createGameDto.DeveloperId}' does not exist");
        }

        var game = createGameDto.ToEntity();

        await context.Games.AddAsync(game);

        await context.SaveChangesAsync();

        var gameDto = game.ToDto();

        if (IncludeHateoasLinks)
        {
            gameDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, game.Id, null);
        }

        return CreatedAtRoute("GetGame", new { gameId = game.Id }, gameDto);
    }
    [HttpPut("{gameId}", Name = "UpdateGame")]
    public async Task<ActionResult> UpdateGame(string gameId, [FromBody] UpdateGameDto updateGameDto, [FromServices] IValidator<UpdateGameDto> validator)
    {

        await validator.ValidateAndThrowAsync(updateGameDto);

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
    [HttpPatch("{gameId}", Name = "PatchGame")]

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

        game.UpdateGame(gameDto);

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{gameId}", Name = "DeleteGame")]
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
    [HttpPut("{gameId}/tags", Name = "UpsertGameTags")]
    public async Task<ActionResult> UpsertGameTags(string gameId, [FromBody] UpsertGameTagDto upsertGameTagDto)
    {
        var game = await context.Games.Include(g => g.GameTags).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var currentTagIds = game.GameTags.Select(gt => gt.TagId).ToHashSet();

        if (currentTagIds.SetEquals(upsertGameTagDto.TagIds))
        {
            return NoContent();
        }

        var existingTags = await context.Tags.Where(t => upsertGameTagDto.TagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync();

        if (existingTags.Count != upsertGameTagDto.TagIds.Count)
        {
            return BadRequest("One Or More Tags Ids Are Invalid");
        }

        game.GameTags.RemoveAll(t => !upsertGameTagDto.TagIds.Contains(t.TagId));

        var tagIdsToAdd = upsertGameTagDto.TagIds.Except(currentTagIds).ToArray();

        game.GameTags.AddRange(tagIdsToAdd.Select(t => new Entities.GameTag()
        {
            GameId = gameId,
            TagId = t,
            CreatedAtUtc = DateTime.UtcNow
        }));

        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPut("{gameId}/genres", Name = "UpsertGameGenres")]
    public async Task<ActionResult> UpsertGameGenres(string gameId, [FromBody] UpsertGameGenresDto upsertGameGenresDto)
    {
        var game = await context.Games.Include(g => g.Genres).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var requestedSlugs = upsertGameGenresDto.GenreSlugs.Select(s => s.ToLowerInvariant()).ToHashSet();

        var currentSlugs = game.Genres.Select(g => g.Slug.ToLowerInvariant()).ToHashSet();

        if (currentSlugs.SetEquals(requestedSlugs))
        {
            return NoContent();
        }

        var targetGenres = await context.Genres.Where(g => requestedSlugs.Contains(g.Slug.ToLower())).ToListAsync();

        if (targetGenres.Count != requestedSlugs.Count)
        {
            return BadRequest("One or more genre slugs are invalid");
        }

        game.Genres.RemoveAll(g => !requestedSlugs.Contains(g.Slug.ToLowerInvariant()));

        var genresToAdd = targetGenres.Where(g => !currentSlugs.Contains(g.Slug.ToLowerInvariant()));

        game.Genres.AddRange(genresToAdd);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
