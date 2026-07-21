namespace Stim.Api.Models.Developer;

public class CreateDeveloperDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
}
