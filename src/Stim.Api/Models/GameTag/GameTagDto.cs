namespace Stim.Api.Models.GameTag;

public class GameTagDto
{
    public required string GameId { get; set; }
    public string TagId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

}
