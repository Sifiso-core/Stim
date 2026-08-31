using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Filters;
using Stim.Api.Models.Comment;
using Stim.Api.Models.Commnet;
using Stim.Api.Models.Common;
using Stim.Api.Services.Concurrency;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Representation_Context;
using Stim.Api.Services.Sorting;
using Stim.Api.Services.User_Context;

namespace Stim.Api.Controllers;


[Route("games/{gameId}/comments")]
[ApiController]
[ApiVersion(1.0)]

public class CommentsController(ApplicationDbContext dbContext, UserContext userContext, IHateoasLinkBuilder<CommentDto, CommentQueryParameters> hateoasLinkBuilder, IConcurrencyService concurrencyService, IRepresentationContext representationContext) : ControllerBase
{
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet(Name = "GetGameComments")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CommentDto>))]

    public async Task<ActionResult<IEnumerable<CommentDto>>> GetGameComments(
        string gameId,
        [FromQuery] CommentQueryParameters queries,
        CancellationToken cancellationToken = default)
    {
        var gameExists = await dbContext.Games.AnyAsync(g => g.Id == gameId, cancellationToken);

        if (!gameExists)
        {
            return NotFound("Game not found.");
        }

        var paginationResult = await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.GameId == gameId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(CommentQueries.ProjectToDto())
            .ToPaginationResultAsync(queries.Page, queries.PageSize);

        if (representationContext.IncludeHateoasLinks)
        {
            foreach (var comment in paginationResult.Data)
            {
                comment.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, comment.Id, null);
            }
        }

        var result = new DataCollectionResponse<CommentDto>()
        {
            Data = paginationResult.Data,

            Links = representationContext.IncludeHateoasLinks ? hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage) : null
        };

        return Ok(result);
    }
    [Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
    [HttpGet("{commentId}", Name = "GetCommentById")]
    [ETagCache]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentDto>> GetCommentById(
    string gameId,
    string commentId,
    CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Comments.AsNoTracking()
            .Where(c => c.GameId == gameId && c.Id == commentId)
            .Select(c => new
            {
                Dto = new CommentDto
                {
                    Id = c.Id,
                    AuthorName = c.User.Name,
                    GameId = c.GameId,
                    UserId = c.UserId,
                    CreatedAtUtc = c.CreatedAtUtc,
                    CommentText = c.CommentText,
                    UpdatedAtUtc = c.UpdatedAtUtc
                },
                c.RowVersion
            }).FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return NotFound();
        }
        HttpContext.Items[HttpContextItemKeys.ResourceVersion] = result.RowVersion;

        if (representationContext.IncludeHateoasLinks)
        {
            result.Dto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, result.Dto.Id, null);
        }

        return Ok(result.Dto);
    }
    [HttpPost(Name = "CreateComment")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CommentDto))]
    public async Task<ActionResult<CommentDto>> CreateComment(
        string gameId,
        [FromBody] CreateCommentDto createCommentDto,
        CancellationToken cancellationToken = default)
    {
        var userId = await userContext.GetUserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        var gameExists = await dbContext.Games.AnyAsync(g => g.Id == gameId, cancellationToken);

        if (!gameExists)
        {
            return NotFound("Game not found.");
        }

        var comment = createCommentDto.ToEntity(gameId, userId);

        dbContext.Comments.Add(comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        var authorName = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var commentDto = comment.ToDto(authorName!);

        if (representationContext.IncludeHateoasLinks)
        {
            commentDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, commentDto.Id, null);
        }

        return CreatedAtRoute("GetCommentById", new { gameId = comment.GameId, commentId = comment.Id }, commentDto);
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{commentId}", Name = "UpdateComment")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateComment(
        string gameId,
        string commentId,
        [FromBody] UpdateCommentDto updateCommentDto,
        CancellationToken cancellationToken = default)
    {
        var userId = await userContext.GetUserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.GameId == gameId, cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        if (comment.UserId != userId)
        {
            return Forbid();
        }

        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(dbContext, comment, expectedVersion);

        comment.UpdateComment(updateCommentDto.Comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{commentId}", Name = "DeleteComment")]
    [RequireIfMatch]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteComment(
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var domainUserId = await userContext.GetUserIdAsync(cancellationToken);
        if (domainUserId is null)
        {
            return Unauthorized();
        }

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        if (comment.UserId != domainUserId)
        {
            return Forbid();
        }
        var expectedVersion = concurrencyService.GetExpectedVersion(HttpContext);

        concurrencyService.SetOriginalVersion(dbContext, comment, expectedVersion);

        dbContext.Comments.Remove(comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

