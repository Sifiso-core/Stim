namespace Stim.Api.Models.Tag;

public static class TagMappings
{
    public static Entities.Tag ToEntity(this CreateTagDto dto)
    {
        return new Entities.Tag()
        {
            Id = $"t_{Guid.CreateVersion7()}",
            Name = dto.Name,
            CreatedAtUtc = DateTime.UtcNow,
            Description = dto.Description,
        };
    }
    public static TagDto ToDto(this Entities.Tag tag)
    {
        return new TagDto()
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description
        };
    }
    public static void UpdateTag(this Entities.Tag tag, UpdateTagDto dto)
    {
        tag.LastUpdatedAtUtc = DateTime.UtcNow;

    }
}