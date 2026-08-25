using Stim.Api.Controllers;
using Stim.Api.Models.Common;
using Stim.Api.Models.Game;

namespace Stim.Api.Services.Hateoas.Game;

public class GameLinkBuilder(LinkGenerator linkGenerator) : IHateoasLinkBuilder<GameDto, GameQueryParameters>
{
    public List<LinkDto> CreateLinksForCollection(HttpContext httpContext, GameQueryParameters gameQueryParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>
        {
            CreateLink(httpContext, nameof(GamesController.GetGames), IanaLinkRelations.Self, HttpMethods.Get, gameQueryParameters),

            CreateLink(httpContext, nameof(GamesController.CreateGame), IanaLinkRelations.Create, HttpMethods.Post)
        };

        if (hasNext)
        {
            links.Add(CreateLink(httpContext, nameof(GamesController.GetGames), IanaLinkRelations.Next, HttpMethods.Get, gameQueryParameters with { Page = gameQueryParameters.Page + 1 }));
        }

        if (hasPrevious)
        {
            links.Add(CreateLink(httpContext, nameof(GamesController.GetGames), IanaLinkRelations.Prev, HttpMethods.Get, gameQueryParameters with { Page = gameQueryParameters.Page - 1 }));
        }

        return links;
    }

    public List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields) => [
        CreateLink(httpContext, nameof(GamesController.GetGame), IanaLinkRelations.Self, HttpMethods.Get, new { gameId = id, fields }),
        CreateLink(httpContext, nameof(GamesController.UpdateGame), IanaLinkRelations.Update, HttpMethods.Put, new { gameId = id }),
        CreateLink(httpContext, nameof(GamesController.PatchGame), IanaLinkRelations.Patch, HttpMethods.Patch, new { gameId = id }),
        CreateLink(httpContext, nameof(GamesController.DeleteGame), IanaLinkRelations.Delete, HttpMethods.Delete, new { gameId = id }),
        CreateLink(httpContext, nameof(GamesController.UpsertGameTags), "upsert-tags", HttpMethods.Put, new { gameId = id }),
        CreateLink(httpContext, nameof(GamesController.UpsertGameGenres), "upsert-genres", HttpMethods.Put, new { gameId = id })
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
