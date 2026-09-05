using Newtonsoft.Json;

namespace Stim.Api.Models.Common;

public class CursorPaginationResult<T>
{
    public List<T> Data { get; set; } = [];
    public int PageSize { get; set; }
    public string? NextCursor { get; set; }
    public string? PreviousCursor { get; set; }

    public bool HasNextPage => !string.IsNullOrEmpty(NextCursor);
    public bool HasPreviousPage => !string.IsNullOrEmpty(PreviousCursor);
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto> Links { get; set; } = [];

}
