using System;

namespace Stim.Api.Services.Representation_Context;

public interface IRepresentationContext
{
    bool IncludeHateoasLinks { get; }
}
