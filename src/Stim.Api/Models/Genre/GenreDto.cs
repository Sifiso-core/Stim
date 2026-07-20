namespace Stim.Api.Models.Genre;

public class GenreDto
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Slug { get; set; } = string.Empty;

}
