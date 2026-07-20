using System.Linq.Expressions;
using Stim.Api.Models.Genre;

namespace Stim.Api.Models.Game;

public static class GameQueries
{
    public static Expression<Func<Entities.Game, GameDto>> ProjectToGameDto()
    {
        return g => new GameDto
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Description,
            DeveloperId = g.DeveloperId,
            ImageUrl = g.ImageUrl,
            Price = g.Price,
            ReleaseDateUtc = g.ReleaseDateUtc,
            LastUpdatedAtUtc = g.LastUpdatedAtUtc,
            Genres = g.Genres.Select(ge => new GenreDto()
            {
                Id = ge.Id,
                Name = ge.Name,
                Slug = ge.Slug
            }).ToList(),
            Tags = g.Tags.Select(t => new Tag.TagDto()
            {
                Id = t.Id,
                Name = t.Name
            }).ToList(),
        };
    }
}
