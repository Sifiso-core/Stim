using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Data;
using Stim.Api.Models.Commnet;
using Stim.Api.Models.Common;
using Stim.Api.Services.Hateoas;
using Stim.Api.Services.Sorting;
using Stim.Api.Services.User_Context;

namespace Stim.Api.Controllers;

[Route("games/{gameId}/comments")]
[ApiController]
[ApiVersion(1.0)]

public class CommentsController(ApplicationDbContext dbContext, UserContext userContext, IHateoasLinkBuilder<CommentDto, CommentQueryParameters> hateoasLinkBuilder) : ControllerBase
{
    private bool IncludeHateoasLinks => Request.Headers.Accept.Contains(CustomMediaTypeNames.Application.HateoasJsonMediaType);
    [HttpGet(Name = "GetGameComments")]
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

        if (IncludeHateoasLinks)
        {
            foreach (var comment in paginationResult.Data)
            {
                comment.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, comment.Id, null);
            }
        }

        var result = new DataCollectionResponse<CommentDto>()
        {
            Data = paginationResult.Data,

            Links = IncludeHateoasLinks ? hateoasLinkBuilder.CreateLinksForCollection(HttpContext, queries, paginationResult.HasNextPage, paginationResult.HasPreviousPage) : null
        };

        return Ok(result);
    }
    [HttpGet("{commentId}", Name = "GetCommentById")]
    public async Task<ActionResult<CommentDto>> GetCommentById(
    string gameId,
    string commentId,
    CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.GameId == gameId && c.Id == commentId)
            .Select(CommentQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        if (IncludeHateoasLinks)
        {
            comment.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, comment.Id, null);
        }

        return Ok(comment);
    }
    [HttpPost(Name = "CreateComment")]
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

        if (IncludeHateoasLinks)
        {
            commentDto.Links = hateoasLinkBuilder.CreateLinksForResource(HttpContext, commentDto.Id, null);
        }

        return CreatedAtRoute("GetCommentById", new { gameId = comment.GameId }, commentDto);
    }
    [HttpPut("{commentId}", Name = "UpdateComment")]
    public async Task<IActionResult> UpdateComment(
        string commentId,
        [FromBody] UpdateCommentDto updateCommentDto,
        CancellationToken cancellationToken = default)
    {
        var userId = await userContext.GetUserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        if (comment.UserId != userId)
        {
            return Forbid();
        }

        comment.UpdateComment(updateCommentDto.Comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    [HttpDelete("{commentId}", Name = "DeleteComment")]
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

        dbContext.Comments.Remove(comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

