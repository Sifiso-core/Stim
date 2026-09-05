using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Stim.Api.Entities;
namespace Stim.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ETagConcurrencyFilterAttribute : Attribute, IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await next();
            return;
        }

        if (context.Result is not ObjectResult objectResult ||
            (objectResult.StatusCode.HasValue && objectResult.StatusCode.Value != StatusCodes.Status200OK))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Items.TryGetValue(HttpContextItemKeys.ResourceVersion, out var versionObject) ||
            versionObject is not uint xmin)
        {
            await next();
            return;
        }

        var etag = new EntityTagHeaderValue($"\"{xmin}\"");
        var etagString = etag.ToString();

        response.Headers[HeaderNames.ETag] = etagString;
        response.Headers[HeaderNames.CacheControl] = "private, must-revalidate";

        if (request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var clientETagHeader) &&
            EntityTagHeaderValue.TryParseList(clientETagHeader, out var clientETags))
        {
            var isMatch = clientETags.Any(t =>
                t.Tag == EntityTagHeaderValue.Any.Tag ||
                t.Tag.Equals(etag.Tag, StringComparison.Ordinal));

            if (isMatch)
            {
                response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }
        }

        await next();
    }
}
