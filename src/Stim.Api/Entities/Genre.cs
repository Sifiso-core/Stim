namespace Stim.Api.Entities;

public class Genre
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
