using Stim.Api.Models.Common;

namespace Stim.Api.Services.Representation_Context;

public sealed class RepresentationContext(IHttpContextAccessor httpContextAccessor)
    : IRepresentationContext
{
    public bool IncludeHateoasLinks
    {
        get
        {
            var accept = httpContextAccessor.HttpContext?
                .Request.Headers.Accept.ToString();

            if (string.IsNullOrWhiteSpace(accept))
            {
                return false;
            }

            return accept.Contains(CustomMediaTypeNames.Application.HateoasJson, StringComparison.OrdinalIgnoreCase) ||
                   accept.Contains(CustomMediaTypeNames.Application.HateoasJsonV2, StringComparison.OrdinalIgnoreCase);
        }
    }
}