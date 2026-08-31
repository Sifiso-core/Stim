using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Stim.Api.Data;
using Stim.Api.Extensions;

namespace Stim.Api.Services.User_Context;

public class UserContext(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext, IMemoryCache memoryCache)
{
    private const string CacheKeyPrefix = "users:id:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        var identityId = httpContextAccessor.HttpContext?.User.GetIdentityId();

        if (identityId is null)
        {
            return null;
        }

        var cacheKey = $"{CacheKeyPrefix}{identityId}";

        var userId = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);
            var userId = await dbContext.Users.Where(u => u.IdentityId == identityId).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            return userId;
        });

        return userId;
    }
}

