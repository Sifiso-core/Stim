namespace Stim.Api.Entities;

public interface ICursorPaginatedEntity
{
    public string Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}