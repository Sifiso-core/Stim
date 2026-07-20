namespace Stim.Api.Models.Genre;

public class UpdateGenreDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
