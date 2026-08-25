using System;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Services.Data_Shaping;

namespace Stim.Api.Models.Common;

public class DataCollectionResponse<T>
{
    public required List<T> Data { get; set; }
    public List<LinkDto> Links { get; set; } = [];
}
