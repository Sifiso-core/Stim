using Stim.Api.Models.Common;

namespace Stim.Api.Services.Hateoas;

public interface IHateoasLinkBuilder<TDto, TQueryParameters>
{
    List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields);
    List<LinkDto> CreateLinksForCollection(HttpContext httpContext, TQueryParameters queryParameters, bool hasNext, bool hasPrevious);
}