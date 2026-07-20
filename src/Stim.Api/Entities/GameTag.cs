namespace Stim.Api.Entities;

public class GameTag
{
    public required string GameId { get; set; }
    public string TagId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

}
