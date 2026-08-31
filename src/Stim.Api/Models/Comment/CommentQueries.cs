using System.Linq.Expressions;
using Stim.Api.Models.Commnet;

namespace Stim.Api.Models.Comment;

public static class CommentQueries
{
    public static Expression<Func<Entities.Comment, CommentDto>> ProjectToDto()
    {
        return c => new CommentDto
        {
            Id = c.Id,
            AuthorName = c.User.Name,
            GameId = c.GameId,
            UserId = c.UserId,
            CreatedAtUtc = c.CreatedAtUtc,
            CommentText = c.CommentText,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }
}
