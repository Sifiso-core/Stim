using Newtonsoft.Json;

namespace Stim.Api.Models.Common;

public class PaginationResult<T>
{
    public List<T> Data { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto>? Links { get; set; }

}