using Stim.Api.Controllers;
using Stim.Api.Models.Common;
using Stim.Api.Models.Tag;

namespace Stim.Api.Services.Hateoas.Tag;

public class TagLinkBuilder(LinkGenerator linkGenerator) : IHateoasLinkBuilder<TagDto, TagQueryParameters>
{
    public List<LinkDto> CreateLinksForCollection(HttpContext httpContext, TagQueryParameters queryParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>()
        {
            CreateLink(httpContext,nameof(TagsController.GetTags),IanaLinkRelations.Self,HttpMethods.Get,queryParameters.Fields),
            CreateLink(httpContext,nameof(TagsController.CreateTag),IanaLinkRelations.Create,HttpMethods.Post,null),
        };

        if (hasNext)
        {
            links.Add(CreateLink(httpContext, nameof(TagsController.GetTags), IanaLinkRelations.Next, HttpMethods.Get, queryParameters with { Page = queryParameters.Page + 1 }));
        }

        if (hasPrevious)
        {
            links.Add(CreateLink(httpContext, nameof(TagsController.GetTags), IanaLinkRelations.Prev, HttpMethods.Get, queryParameters with { Page = queryParameters.Page - 1 }));
        }

        return links;
    }

    public List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields) => [
        CreateLink(httpContext,nameof(TagsController.GetTag),IanaLinkRelations.Self,HttpMethods.Get,new{tagId = id}),
        CreateLink(httpContext,nameof(TagsController.UpdateTag),IanaLinkRelations.Update,HttpMethods.Put,new{tagId = id}),
        CreateLink(httpContext,nameof(TagsController.DeleteTag),IanaLinkRelations.Delete,HttpMethods.Delete,new{tagId = id})
    ];

    private LinkDto CreateLink(HttpContext httpContext, string routeName, string rel, string method, object? values = null)
    {
        var href = linkGenerator.GetUriByRouteValues(httpContext, routeName, values);
        return new LinkDto
        {
            Href = href ?? throw new InvalidOperationException($"Invalid route name: '{routeName}'"),
            Rel = rel,
            Method = method
        };
    }
}
