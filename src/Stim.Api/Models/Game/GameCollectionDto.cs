using System;

namespace Stim.Api.Models.Game;

public class GameCollectionDto
{
    public required List<GameDto> Data { get; set; }
}
