using System.Linq.Expressions;

namespace Stim.Api.Models.Commnet;

public static class CommentQueries
{
    public static Expression<Func<Entities.Comments, CommentDto>> ProjectToDto()
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
