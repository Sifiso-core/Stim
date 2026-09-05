namespace Stim.Api.Entities;

public class GameGenre
{
    public string GameId { get; set; } = string.Empty;
    public Game Game { get; set; } = null!;
    public string GenreId { get; set; } = string.Empty;
    public Genre Genre { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}