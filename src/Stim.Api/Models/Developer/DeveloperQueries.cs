using System.Linq.Expressions;
using Stim.Api.Models.Game;

namespace Stim.Api.Models.Developer;

public static class DeveloperQueries
{
    public static Expression<Func<Entities.Developer, DeveloperDto>> ProjectToDto()
    {
        return d => new DeveloperDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Games = d.Games.Select(g => new GameDto()
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
                DeveloperId = g.DeveloperId,
                Price = g.Price,
                ImageUrl = g.ImageUrl,
                LastUpdatedAtUtc = g.LastUpdatedAtUtc,
                ReleaseDateUtc = g.ReleaseDateUtc,
                Tags = g.Tags.Select(t => new Tag.TagDto()
                {
                    Id = t.Id,
                    Name = t.Name
                }).ToList(),
                Genres = g.Genres.Select(g => new Genre.GenreDto()
                {
                    Id = g.Id,
                    Description = g.Description,
                    ImageUrl = g.ImageUrl,
                    Name = g.Name,
                    Slug = g.Slug
                }).ToList()
            }).ToList(),
            WebsiteUrl = d.WebsiteUrl

        };
    }
}
