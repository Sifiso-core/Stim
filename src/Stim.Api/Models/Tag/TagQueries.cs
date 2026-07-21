using System.Linq.Expressions;

namespace Stim.Api.Models.Tag;

public static class TagQueries
{
    public static Expression<Func<Entities.Tag, TagDto>> ProjectToDto()
    {
        return t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description
        };
    }
}
