using System;
using Newtonsoft.Json;
using Stim.Api.Models.Common;

namespace Stim.Api.Models.Commnet;

public class CommentDto()
{
    public string Id { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto>? Links { get; set; }

};
