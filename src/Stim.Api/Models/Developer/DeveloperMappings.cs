using Stim.Api.Models.Game;
using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;

namespace Stim.Api.Models.Developer;

public static class DeveloperMappings
{
    public static Entities.Developer ToEntity(this CreateDeveloperDto dto)
    {
        return new Entities.Developer()
        {
            Id = $"d_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            WebsiteUrl = dto.WebsiteUrl
        };
    }
    public static DeveloperDto ToDto(this Entities.Developer developer)
    {
        return new DeveloperDto()
        {
            Id = developer.Id,
            Name = developer.Name,
            Description = developer.Description,
            WebsiteUrl = developer.WebsiteUrl,
            Games = [.. developer.Games.Select(g => new GameDto(){
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
                DeveloperId = g.DeveloperId,
                ImageUrl= g.ImageUrl,
                LastUpdatedAtUtc = g.LastUpdatedAtUtc,
                Price = g.Price,
                ReleaseDateUtc = g.ReleaseDateUtc,
                Tags = [.. g.Tags.Select(t => new TagDto(){
                    Id  = t.Id,
                    Name = t.Name
                })],
                Genres = [.. g.Genres.Select(gn => new GenreDto(){
                    Id = gn.Id,
                    Description = gn.Description,
                    ImageUrl = gn.ImageUrl,
                    Name = gn.Name,
                    Slug = gn.Slug
                })]
            })]
        };
    }
    public static void UpdateDeveloper(this Entities.Developer developer, UpdateDeveloperDto updateDeveloper)
    {
        developer.Description = updateDeveloper.Description;
        developer.Name = updateDeveloper.Name;
        developer.WebsiteUrl = updateDeveloper.WebsiteUrl;
    }
    public static void UpdateDeveloper(this Entities.Developer developer, DeveloperDto updateDeveloper)
    {
        developer.Description = updateDeveloper.Description;
        developer.Name = updateDeveloper.Name;
        developer.WebsiteUrl = updateDeveloper.WebsiteUrl;
    }
}