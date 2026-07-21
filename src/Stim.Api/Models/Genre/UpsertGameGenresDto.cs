namespace Stim.Api.Models.Genre;

public class UpsertGameGenresDto
{
    public required List<string> GenreSlugs { get; set; }
}
