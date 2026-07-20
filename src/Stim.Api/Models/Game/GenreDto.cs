namespace Stim.Api.Models.Game;

public class GenreDto
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

}
