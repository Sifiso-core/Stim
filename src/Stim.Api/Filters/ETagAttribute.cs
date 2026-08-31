using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Stim.Api.Entities;
namespace Stim.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ETagCacheAttribute : Attribute, IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        var response = context.HttpContext.Response;

        if (request.Method != HttpMethods.Get)
        {
            await next();
            return;
        }

        if (context.Result is not ObjectResult
            {
                StatusCode: null or StatusCodes.Status200OK
            })
        {
            await next();
            return;
        }

        if (!context.HttpContext.Items.TryGetValue(HttpContextItemKeys.ResourceVersion, out var versionObject) || versionObject is not uint version)
        {
            await next();
            return;
        }

        var etag = $"\"{version}\"";

        // Check whether the client already has this representation.
        if (request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var clientETag))
        {
            var clientVersion = clientETag
                .ToString()
                .Trim()
                .Trim('"');

            if (clientVersion == version.ToString())
            {
                response.Headers.ETag = etag;
                response.Headers.CacheControl = "private, must-revalidate";

                context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);

                return;
            }
        }

        response.Headers.ETag = etag;

        response.Headers.CacheControl = "private, must-revalidate";

        await next();
    }
}
