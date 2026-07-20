namespace Stim.Api.Entities;

public class Developer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public ICollection<Game> Games { get; set; } = [];

}
