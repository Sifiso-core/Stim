using System;

namespace Stim.Api.Models.Common;

public class DataCollectionResponse<T>
{
    public required List<T> Data { get; set; }
}
