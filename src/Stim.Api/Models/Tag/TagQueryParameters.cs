using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Tag;

public class TagQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Sort { get; set; }
}
