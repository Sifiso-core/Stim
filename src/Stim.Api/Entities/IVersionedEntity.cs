namespace Stim.Api.Entities;

public interface IVersionedEntity
{
    uint RowVersion { get; }
}