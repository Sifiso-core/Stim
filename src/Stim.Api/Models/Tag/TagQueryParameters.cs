using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Tag;

public class TagQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
