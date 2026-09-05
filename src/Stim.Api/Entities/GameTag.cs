namespace Stim.Api.Entities;

public class GameTag
{
    public required string GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string TagId { get; set; } = string.Empty;
    public Tag Tag { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }

}
