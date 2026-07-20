using Stim.Api.Models.Tag;

namespace Stim.Api.Models.Game;

public static class GameMappings
{
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
            GameTags = [.. game.GameTags.Select(gt => new GameTag.GameTagDto()
            {
                GameId = gt.GameId,
                CreatedAtUtc = gt.CreatedAtUtc,
                TagId = gt.TagId
            })],
            Genres = [.. game.Genres.Select(g => new GenreDto(){
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug
            })],
            Tags = [.. game.Tags.Select(t => new TagDto(){
                Id = t.Id,
                Name = t.Name
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
}