namespace Stim.Api.Entities;

public class Comment : IVersionedEntity
{
    public string Id { get; set; } = string.Empty;
    public required string CommentText { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required string GameId { get; set; }
    public required string UserId { get; set; }
    public User User { get; set; } = null!;

    public uint RowVersion { get; set; }
}
