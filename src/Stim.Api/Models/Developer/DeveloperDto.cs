using Newtonsoft.Json;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;

namespace Stim.Api.Models.Developer;

public class DeveloperDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public ICollection<GameDto> Games { get; set; } = [];
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto>? Links { get; set; }
}
