using Stim.Api.Controllers;
using Stim.Api.Models.Common;
using Stim.Api.Models.Genre;

namespace Stim.Api.Services.Hateoas.Genre;

public class GenreLinkBuilder(LinkGenerator linkGenerator) : IHateoasLinkBuilder<GenreDto, GenreQueryParameters>
{
    public List<LinkDto> CreateLinksForCollection(HttpContext httpContext, GenreQueryParameters queryParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>
        {
            CreateLink(httpContext, nameof(GenresController.GetGenres), IanaLinkRelations.Self, HttpMethods.Get, queryParameters),
            CreateLink(httpContext, nameof(GenresController.CreateGenre), IanaLinkRelations.Create, HttpMethods.Post)
        };

        if (hasNext)
        {
            links.Add(CreateLink(httpContext, nameof(GenresController.GetGenres), IanaLinkRelations.Next, HttpMethods.Get, queryParameters with { Page = queryParameters.Page + 1 }));
        }

        if (hasPrevious)
        {
            links.Add(CreateLink(httpContext, nameof(GenresController.GetGenres), IanaLinkRelations.Prev, HttpMethods.Get, queryParameters with { Page = queryParameters.Page - 1 }));
        }

        return links;
    }

    public List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields) => [
        CreateLink(httpContext, nameof(GenresController.GetGenreBySlugOrId), IanaLinkRelations.Self, HttpMethods.Get, new { identifier = id, fields }),
        CreateLink(httpContext, nameof(GenresController.UpdateGenre), IanaLinkRelations.Update, HttpMethods.Put, new { genreId = id }),
        CreateLink(httpContext, nameof(GenresController.DeleteGenre), IanaLinkRelations.Delete, HttpMethods.Delete, new { genreId = id }),
        CreateLink(httpContext, nameof(GenresController.GetGamesByGenreSlug), "games", HttpMethods.Get, new { slug = id })
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
