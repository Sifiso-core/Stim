using Stim.Api.Models.Genre;
using Stim.Api.Models.Tag;
using Stim.Api.Services.Sorting;

namespace Stim.Api.Models.Game;

public static class GameMappings
{
    public static readonly SortMappingDefinition<GameDto, Entities.Game> SortMapping = new()
    {
        Mappings = [
            new(nameof(GameDto.Title),nameof(Entities.Game.Title)),
            new(nameof(GameDto.Price),nameof(Entities.Game.Price)),
            new(nameof(GameDto.ReleaseDateUtc),nameof(Entities.Game.ReleaseDateUtc)),
            new(nameof(GameDto.LastUpdatedAtUtc),nameof(Entities.Game.LastUpdatedAtUtc)),
        ]
    };
    public static List<GameDto> ToDto(this IEnumerable<Entities.Game> games)
    {
        return [.. games.Select(g => g.ToDto())];
    }
    public static GameDto ToDto(this Entities.Game game)
    {
        return new GameDto()
        {
            Id = game.Id,
            Title = game.Title,
            Description = game.Description,
            DeveloperId = game.DeveloperId,
            ImageUrl = game.ImageUrl,
            Price = game.Price,
            ReleaseDateUtc = game.ReleaseDateUtc,
            LastUpdatedAtUtc = game.LastUpdatedAtUtc,
            Genres = [.. game.GameGenres.Select(gg => new GenreDto
            {
                Id = gg.Genre.Id, Name = gg.Genre.Name, Slug = gg.Genre.Slug
            })],
            Tags = [.. game.GameTags.Select(t => new TagDto(){
                Id = t.Tag.Id,
                Name = t.Tag.Name,
                Description = t.Tag.Description
            })],
        };
    }
    public static Entities.Game ToEntity(this CreateGameDto createGameDto)
    {
        return new Entities.Game()
        {
            Id = $"g_{Guid.CreateVersion7()}",
            Title = createGameDto.Title,
            Description = createGameDto.Description,
            DeveloperId = createGameDto.DeveloperId,
            ImageUrl = createGameDto.ImageUrl,
            Price = createGameDto.Price,
            ReleaseDateUtc = createGameDto.ReleaseDateUtc,
        };
    }
    public static void UpdateGame(this Entities.Game game, UpdateGameDto updateGameDto)
    {
        game.Title = updateGameDto.Title;
        game.Description = updateGameDto.Description;
        game.DeveloperId = updateGameDto.DeveloperId;
        game.ImageUrl = updateGameDto.ImageUrl;
        game.Price = updateGameDto.Price;
        game.ReleaseDateUtc = updateGameDto.ReleaseDateUtc;
        game.LastUpdatedAtUtc = DateTime.UtcNow;
    }
    public static void UpdateGame(this Entities.Game game, GameDto gameDto)
    {
        game.Title = gameDto.Title;
        game.Description = gameDto.Description;
        game.DeveloperId = gameDto.DeveloperId;
        game.ImageUrl = gameDto.ImageUrl;
        game.Price = gameDto.Price;
        game.ReleaseDateUtc = gameDto.ReleaseDateUtc;
        game.LastUpdatedAtUtc = DateTime.UtcNow;
    }
}