using Microsoft.AspNetCore.Mvc;
using Stim.Api.Models.Common;

namespace Stim.Api.Models.Genre;

public record GenreQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Slug { get; set; }
    public string? Sort { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Fields { get; set; }
    public string? Cursor { get; set; }
    public PaginationType PaginationType { get; set; }
    public static class GenreQueryDefaults
    {
        public const int Page = 1;
        public const int PageSize = 30;
    }
}


