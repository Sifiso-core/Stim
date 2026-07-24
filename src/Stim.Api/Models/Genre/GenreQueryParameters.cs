using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Genre;

public class GenreQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Slug { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}