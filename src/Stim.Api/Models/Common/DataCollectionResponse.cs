using Newtonsoft.Json;

namespace Stim.Api.Models.Common;

public class DataCollectionResponse<T>
{
    public required List<T> Data { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto>? Links { get; set; }
}
