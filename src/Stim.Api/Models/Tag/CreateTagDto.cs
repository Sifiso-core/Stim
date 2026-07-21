namespace Stim.Api.Models.Tag;

public class CreateTagDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
public class UpdateTagDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
