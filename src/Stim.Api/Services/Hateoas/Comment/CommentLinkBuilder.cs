using Stim.Api.Controllers;
using Stim.Api.Models.Commnet;
using Stim.Api.Models.Common;

namespace Stim.Api.Services.Hateoas.Comment;

public class CommentLinkBuilder(LinkGenerator linkGenerator) : IHateoasLinkBuilder<CommentDto, CommentQueryParameters>
{
    public List<LinkDto> CreateLinksForCollection(HttpContext httpContext, CommentQueryParameters queryParameters, bool hasNext, bool hasPrevious)
    {
        var gameId = httpContext.GetRouteValue("gameId")?.ToString();
        var links = new List<LinkDto>()
        {
            CreateLink(httpContext,nameof(CommentsController.GetGameComments),IanaLinkRelations.Self,HttpMethods.Get,new{gameId}),
            CreateLink(httpContext,nameof(CommentsController.CreateComment),IanaLinkRelations.Create,HttpMethods.Post,new{gameId}),
        };

        if (hasNext)
        {
            links.Add(CreateLink(httpContext, nameof(CommentsController.GetGameComments), IanaLinkRelations.Next, HttpMethods.Get, queryParameters with { Page = queryParameters.Page + 1 }));
        }

        if (hasPrevious)
        {
            links.Add(CreateLink(httpContext, nameof(CommentsController.GetGameComments), IanaLinkRelations.Prev, HttpMethods.Get, queryParameters with { Page = queryParameters.Page - 1 }));
        }

        return links;
    }

    public List<LinkDto> CreateLinksForResource(HttpContext httpContext, string id, string? fields)
    {
        var gameId = httpContext.GetRouteValue("gameId")?.ToString();
        return [
        CreateLink(httpContext,nameof(CommentsController.UpdateComment),IanaLinkRelations.Update,HttpMethods.Put, new { gameId,commentId = id }),
        CreateLink(httpContext,nameof(CommentsController.DeleteComment),IanaLinkRelations.Delete,HttpMethods.Delete,new{gameId,commentId = id})
        ];
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