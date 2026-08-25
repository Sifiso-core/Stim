using Stim.Api.Models.Common;

namespace Stim.Api.Models.Tag;

public class TagDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public List<LinkDto> Links { get; set; } = [];

}
