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
using Stim.Api.Models.Game;
using Stim.Api.Models.GameTag;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Concurrency;
using Stim.Api.Services.Data_Shaping;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Representation_Context;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Controllers;


[Route("games")]
[ApiController]
[ApiVersion(1.0)]
public class GamesController(ApplicationDbContext context, IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{


    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [HttpGet(Name = "GetGames")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataCollectionResponse<GameDto>))]
    public async Task<ActionResult<DataCollectionResponse<GameDto>>> GetGames([FromQuery] GameQueryParameters queries,
    [FromServices] SortMappingProvider sortMappingProvider,
    [FromServices] DataShapingService dataShapingService,
    [FromServices] IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder,
    [FromServices] IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder,
      CancellationToken cancellationToken)
    {
        var validationResult = ValidateQueryParameters(queries, sortMappingProvider, dataShapingService);

        if (validationResult is not null) { return validationResult; }

        var gamesQuery = BuildGameQuery(queries, sortMappingProvider);

        var pageSize = queries.PageSize ?? GameQueryParameters.GamesQueryDefaults.PageSize; DataCollectionResponse<GameDto> paginationResult;

        if (queries.PaginationType == PaginationType.Cursor)
        {
            var result = await gamesQuery.ToCursorPaginationResult(queries.Cursor, pageSize, cancellationToken);
            paginationResult = new()
            {
                Data = result.Data.ToDto(),
                Links = result.Links,
                Pagination = result.Pagination
            };
        }
        else
        {
            var page = queries.Page ?? GameQueryParameters.GamesQueryDefaults.Page; var result = await gamesQuery.ToPaginationResultAsync(page, pageSize, cancellationToken);
            queries = queries with { Page = page }; paginationResult = new() { Data = result.Data.ToDto(), Links = result.Links, Pagination = result.Pagination };
        }

        AddHateoasLinks(paginationResult, queries, gameLinkBuilder, genreLinkBuilder, tagLinkBuilder);

        var shapedData = dataShapingService.ShapeCollectionData(paginationResult.Data, queries.Fields);

        var response = new DataCollectionResponse<ExpandoObject> { Data = [.. shapedData], Pagination = paginationResult.Pagination, Links = paginationResult.Links };
        return Ok(response);
    }
    [Authorize(Roles = $"{Roles.Member},{Roles.Admin}")]
    [HttpGet("{gameId}", Name = "GetGame")]
    [ETagConcurrencyFilterAttribute]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GameDto))]
    public async Task<ActionResult<GameDto>> GetGame(string gameId, [FromServices] DataShapingService dataShapingService, string? fields)
    {
        if (!dataShapingService.Validate<GameDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"The provided data shaping field isn't valid: {fields}");
        }
        var game = await context.Games.Include(g => g.GameTags).ThenInclude(t => t.Tag).Include(g => g.GameGenres).ThenInclude(g => g.Genre).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        HttpContext.Items[HttpContextItemKeys.ResourceVersion] = game.RowVersion;

        var gameDto = game.ToDto();

        if (representationContext.IncludeHateoasLinks)
        {
            gameDto.Links = gameLinkBuilder.CreateLinksForResource(HttpContext, gameDto.Id, fields);

        }

        return Ok(gameDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPost(Name = "CreateGame")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(GameDto))]
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

        if (representationContext.IncludeHateoasLinks)
        {
            gameDto.Links = gameLinkBuilder.CreateLinksForResource(HttpContext, game.Id, null);
        }

        return CreatedAtRoute("GetGame", new { gameId = game.Id }, gameDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{gameId}", Name = "UpdateGame")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, game, expectedVersion);

        game.UpdateGame(updateGameDto);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{gameId}", Name = "PatchGame")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> PatchGame(string gameId, JsonPatchDocument<GameDto> document)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, game, expectedVersion);

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
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{gameId}", Name = "DeleteGame")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [RequireIfMatch]
    public async Task<ActionResult> DeleteGame(string gameId)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, game, expectedVersion);

        context.Games.Remove(game);

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{gameId}/tags", Name = "UpsertGameTags")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequireIfMatch]
    public async Task<ActionResult> UpsertGameTags(string gameId, [FromBody] UpsertGameTagDto upsertGameTagDto)
    {
        var game = await context.Games.Include(g => g.GameTags).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, game, expectedVersion);

        var currentTagIds = game.GameTags.Select(gt => gt.TagId).ToHashSet();

        if (currentTagIds.SetEquals(upsertGameTagDto.TagIds))
        {
            return NoContent();
        }

        var existingTags = await context.Tags.Where(t => upsertGameTagDto.TagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync();

        if (existingTags.Count != upsertGameTagDto.TagIds.Count)
        {
            return Problem("One Or More Tags Ids Are Invalid", statusCode: StatusCodes.Status400BadRequest);
        }

        game.GameTags.RemoveAll(t => !upsertGameTagDto.TagIds.Contains(t.TagId));

        var tagIdsToAdd = upsertGameTagDto.TagIds.Except(currentTagIds).ToArray();

        game.GameTags.AddRange(tagIdsToAdd.Select(t => new Entities.GameTag()
        {
            GameId = gameId,
            TagId = t,
            CreatedAtUtc = DateTime.UtcNow
        }));

        game.LastUpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{gameId}/genres", Name = "UpsertGameGenres")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequireIfMatch]
    public async Task<ActionResult> UpsertGameGenres(string gameId, [FromBody] UpsertGameGenresDto upsertGameGenresDto)
    {
        var game = await context.Games.Include(g => g.Genres).FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            return NotFound();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(context, game, expectedVersion);

        var requestedSlugs = upsertGameGenresDto.GenreSlugs.Select(s => s.ToLowerInvariant()).ToHashSet();

        var currentSlugs = game.Genres.Select(g => g.Slug.ToLowerInvariant()).ToHashSet();

        if (currentSlugs.SetEquals(requestedSlugs))
        {
            return NoContent();
        }

        var targetGenres = await context.Genres.Where(g => requestedSlugs.Contains(g.Slug.ToLower())).ToListAsync();

        if (targetGenres.Count != requestedSlugs.Count)
        {
            return Problem("One or more genre slugs are invalid", statusCode: StatusCodes.Status400BadRequest);
        }

        game.Genres.RemoveAll(g => !requestedSlugs.Contains(g.Slug.ToLowerInvariant()));

        var currentGenreIds = game.GameGenres.Select(x => x.GenreId).ToHashSet();

        var genresToRemove = game.GameGenres.Where(x => !requestedSlugs.Contains(x.Genre.Slug.ToLowerInvariant())).ToList();

        foreach (var gameGenre in genresToRemove)
        {
            game.GameGenres.Remove(gameGenre);
        }

        var genresToAdd = targetGenres
            .Where(g => !currentGenreIds.Contains(g.Id))
            .Select(g => new GameGenre
            {
                GameId = game.Id,
                GenreId = g.Id,
                Genre = g,
                CreatedAtUtc = DateTime.UtcNow
            });

        game.GameGenres.AddRange(genresToAdd);

        game.LastUpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
    private IQueryable<Game> BuildGameQuery(GameQueryParameters queries, SortMappingProvider sortMappingProvider)
    {
        var search = queries.Search?.Trim().ToLower();

        var sortMappings = sortMappingProvider.GetMappings<GameDto, Game>();

        return context.Games.Include(g => g.GameGenres).ThenInclude(g => g.Genre).Include(g => g.GameTags).ThenInclude(gt => gt.Tag).Where(game => search == null || game.Title.ToLower().Contains(search))
            .ApplySort(queries.Sort, sortMappings);
    }

    private ObjectResult? ValidateQueryParameters(
        GameQueryParameters queries,
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

        if (!sortMappingProvider.ValidateMappings<GameDto, Game>(queries.Sort))
        {
            return Problem($"The supplied sort parameter is invalid: '{queries.Sort}'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!dataShapingService.Validate<GameDto>(queries.Fields))
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
            return Problem("'Cursor' cannot be used with offset pagination. " + "Review your parameters.", statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
    private void AddHateoasLinks(
        DataCollectionResponse<GameDto> response,
        GameQueryParameters queries,
        IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
        IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder,
        IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder)
    {
        if (!representationContext.IncludeHateoasLinks)
        {
            return;
        }

        foreach (var game in response.Data)
        {
            AddGameLinks(game, queries, gameLinkBuilder, genreLinkBuilder, tagLinkBuilder);
        }

        if (response.Pagination?.PaginationType == PaginationType.Cursor)
        {
            response.Links = gameLinkBuilder.CreateCursorCollectionLinks(HttpContext, queries, response.Pagination.NextCursor, response.Pagination.PreviousCursor);

            return;
        }

        response.Links = gameLinkBuilder.CreateLinksForCollection(HttpContext, queries, response.Pagination?.HasNextPage ?? false, response.Pagination?.HasPreviousPage ?? false);
    }

    private void AddGameLinks(
        GameDto game,
        GameQueryParameters queries,
        IHateoasLinkBuilder<GameDto, GameQueryParameters> gameLinkBuilder,
        IHateoasLinkBuilder<GenreDto, GenreQueryParameters> genreLinkBuilder,
        IHateoasLinkBuilder<TagDto, TagQueryParameters> tagLinkBuilder)
    {
        game.Links = gameLinkBuilder.CreateLinksForResource(HttpContext, game.Id, queries.Fields);


        foreach (var genre in game.Genres)
        {
            genre.Links = genreLinkBuilder.CreateLinksForResource(HttpContext, genre.Id, queries.Fields);
        }

        foreach (var tag in game.Tags)
        {
            tag.Links = tagLinkBuilder.CreateLinksForResource(HttpContext, tag.Id, queries.Fields);
        }
    }
}
