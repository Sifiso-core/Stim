namespace Stim.Api.Models.Game;

public class UpdateGameDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime ReleaseDateUtc { get; set; }
    public string? ImageUrl { get; set; }
    public string DeveloperId { get; set; } = string.Empty;

}
