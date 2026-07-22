using Stim.Api.Extensions;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Models.Genre;

public static class GenreMappings
{
    public static readonly SortMappingDefinition<GenreDto, Entities.Genre> SortMapping = new()
    {
        Mappings = [
        new(nameof(GenreDto.Name),nameof(Entities.Genre.Name)),
            new(nameof(GenreDto.Slug),nameof(Entities.Genre.Slug))
        ]
    };
    public static GenreDto ToDto(this Entities.Genre genre)
    {
        return new GenreDto()
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug,
            Description = genre.Description,
            ImageUrl = genre.ImageUrl
        };
    }
    public static Entities.Genre ToEntity(this CreateGenreDto createGenreDto)
    {
        return new Entities.Genre()
        {
            Id = $"g_{Guid.CreateVersion7()}",
            Name = createGenreDto.Name,
            Slug = createGenreDto.Name.ToSlug(),
            Description = createGenreDto.Description,
            ImageUrl = createGenreDto.ImageUrl,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
    public static void UpdateGenre(this Entities.Genre genre, UpdateGenreDto genreDto)
    {
        genre.Name = genreDto.Name;
        genre.Description = genreDto.Description;
        genre.ImageUrl = genreDto.ImageUrl;
        genre.LastUpdatedAtUtc = DateTime.UtcNow;
    }
}
