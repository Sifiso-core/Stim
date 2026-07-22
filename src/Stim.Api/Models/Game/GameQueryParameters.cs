using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Models.Game;

public class GameQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }

}
