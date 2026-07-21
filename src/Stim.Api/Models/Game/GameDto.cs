using Stim.Api.Models.GameTag;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;

namespace Stim.Api.Models.Game;

public class GameDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime ReleaseDateUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public string? ImageUrl { get; set; }
    public string DeveloperId { get; set; } = string.Empty;
    public List<GenreDto> Genres { get; set; } = [];
    public List<TagDto> Tags { get; set; } = [];

}
