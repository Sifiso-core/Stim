namespace Stim.Api.Entities;

public class Tag : IVersionedEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
    public uint RowVersion { get; set; }
}
