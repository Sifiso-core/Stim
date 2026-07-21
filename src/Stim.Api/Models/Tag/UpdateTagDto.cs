namespace Stim.Api.Models.Tag;

public class UpdateTagDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
