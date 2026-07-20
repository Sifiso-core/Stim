namespace Stim.Api.Entities;

public class Genre
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public string Slug { get; set; } = string.Empty;
}
