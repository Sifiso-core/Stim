using System;
using Stim.Api.Controllers;
using Stim.Api.Models.Common;
using Stim.Api.Models.Developer;

namespace Stim.Api.Services.Hateoas.Developer;

public class DeveloperLinkBuilder(LinkGenerator linkGenerator) : IHateoasLinkBuilder<DeveloperDto, DeveloperQueryParameters>
{
    public List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields) => [
        CreateLink(httpContext, nameof(DevelopersController.GetDeveloper), IanaLinkRelations.Self, HttpMethods.Get, new { developerId = id, fields }),
        CreateLink(httpContext, nameof(DevelopersController.UpdateDeveloper), IanaLinkRelations.Update, HttpMethods.Put, new { developerId = id }),
        CreateLink(httpContext, nameof(DevelopersController.PatchDeveloper), IanaLinkRelations.Patch, HttpMethods.Patch, new { developerId = id }),
        CreateLink(httpContext, nameof(DevelopersController.DeleteDeveloper), IanaLinkRelations.Delete, HttpMethods.Delete, new { developerId = id })
    ];

    public List<LinkDto> CreateLinksForCollection(HttpContext httpContext, DeveloperQueryParameters developerQueryParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>
        {
            CreateLink(httpContext, nameof(DevelopersController.GetDevelopers), IanaLinkRelations.Self, HttpMethods.Get, developerQueryParameters),
            CreateLink(httpContext, nameof(DevelopersController.CreateDeveloper), IanaLinkRelations.Create, HttpMethods.Post)
        };

        if (hasNext)
        {
            links.Add(CreateLink(httpContext, nameof(DevelopersController.GetDevelopers), IanaLinkRelations.Next, HttpMethods.Get, developerQueryParameters with { Page = developerQueryParameters.Page + 1 }));
        }

        if (hasPrevious)
        {
            links.Add(CreateLink(httpContext, nameof(DevelopersController.GetDevelopers), IanaLinkRelations.Prev, HttpMethods.Get, developerQueryParameters with { Page = developerQueryParameters.Page - 1 }));
        }

        return links;
    }

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
