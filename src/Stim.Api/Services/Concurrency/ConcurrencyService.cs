using Stim.Api.Data;
using Stim.Api.Entities;
using Stim.Api.Filters;

namespace Stim.Api.Services.Concurrency;

public sealed class ConcurrencyService : IConcurrencyService
{
    public uint GetExpectedVersion(HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(HttpContextItemKeys.ClientETag, out var value) || value is not uint expectedVersion)
        {
            throw new InvalidOperationException("Expected concurrency version was not provided.");
        }

        return expectedVersion;
    }

    public void SetOriginalVersion<T>(ApplicationDbContext dbContext, T entity, uint expectedVersion) where T : class, IVersionedEntity
    {
        dbContext.Entry(entity).Property(x => x.RowVersion).OriginalValue = expectedVersion;
    }
}