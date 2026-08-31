using System;
using Stim.Api.Data;
using Stim.Api.Entities;

namespace Stim.Api.Services.Concurrency;

public interface IConcurrencyService
{
    uint GetExpectedVersion(HttpContext httpContext);

    void SetOriginalVersion<T>(ApplicationDbContext dbContext, T entity, uint expectedVersion) where T : class, IVersionedEntity;
}
