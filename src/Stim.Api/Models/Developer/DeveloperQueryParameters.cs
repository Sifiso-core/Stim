using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Developer;

public record DeveloperQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Fields { get; set; }

}