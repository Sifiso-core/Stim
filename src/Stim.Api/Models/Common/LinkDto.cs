using System;

namespace Stim.Api.Models.Common;

public class LinkDto
{
    public required string Href { get; set; }
    public required string Rel { get; set; }
    public required string Method { get; set; }
}
