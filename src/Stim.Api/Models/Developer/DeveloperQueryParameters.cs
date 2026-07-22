using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Developer;

public class DeveloperQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Sort { get; set; }
}