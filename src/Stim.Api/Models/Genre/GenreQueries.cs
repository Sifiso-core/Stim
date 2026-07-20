using System.Linq.Expressions;

namespace Stim.Api.Models.Genre;

public static class GenreQueries
{
    public static Expression<Func<Entities.Genre, GenreDto>> ProjectToDto()
    {
        return g => new GenreDto
        {
            Id = g.Id,
            Name = g.Name,
            Slug = g.Slug,
            Description = g.Description,
            ImageUrl = g.ImageUrl
        };
    }
}
