using System;

namespace Stim.Api.Entities;


public class Game
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime ReleaseDateUtc { get; set; }
    public string? ImageUrl { get; set; }
    public string DeveloperId { get; set; } = string.Empty;
    public ICollection<Genre> Genres { get; set; } = [];
    public ICollection<GameTag> GameTags { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];

}
