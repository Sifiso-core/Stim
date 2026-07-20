namespace Stim.Api.Models.Genre;

public class CreateGenreDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
