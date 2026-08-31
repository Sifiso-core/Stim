namespace Stim.Api.Models.Commnet;

public static class CommentMappings
{
    public static Entities.Comment ToEntity(this CreateCommentDto createCommentDto, string gameId, string userId)
    {
        return new Entities.Comment()
        {
            GameId = gameId,
            UserId = userId,
            CommentText = createCommentDto.Comment,
            CreatedAtUtc = DateTime.UtcNow,
            Id = $"c_{Guid.CreateVersion7()}",
        };
    }
    public static CommentDto ToDto(this Entities.Comment comment, string authorName)
    {
        return new CommentDto()
        {
            AuthorName = authorName,
            CreatedAtUtc = comment.CreatedAtUtc,
            GameId = comment.GameId,
            Id = comment.Id,
            CommentText = comment.CommentText,
            UpdatedAtUtc = comment.UpdatedAtUtc,
            UserId = comment.UserId
        };
    }
    public static void UpdateComment(this Entities.Comment comment, string commentText)
    {
        comment.CommentText = commentText;
        comment.UpdatedAtUtc = DateTime.UtcNow;
    }
}