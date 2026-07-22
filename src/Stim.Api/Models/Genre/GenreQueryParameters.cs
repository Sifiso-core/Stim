using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Genre;

public class GenreQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Slug { get; set; }
}