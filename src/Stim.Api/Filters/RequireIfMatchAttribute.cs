using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
namespace Stim.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireIfMatchAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatch) || string.IsNullOrWhiteSpace(ifMatch))
        {
            context.Result = new ObjectResult(new
            {
                error = "An If-Match header containing the current ETag is required."
            })
            {
                StatusCode = StatusCodes.Status428PreconditionRequired
            };

            return;
        }

        var rawETag = ifMatch.ToString().Trim();

        if (!rawETag.StartsWith('"') || !rawETag.EndsWith('"'))
        {
            context.Result = new ObjectResult(new
            {
                error = "The If-Match header contains an invalid ETag."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            return;
        }

        var cleanETag = rawETag.Trim('"');

        if (!uint.TryParse(cleanETag, out var expectedVersion))
        {
            context.Result = new ObjectResult(new
            {
                error = "The If-Match header contains an invalid ETag."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            return;
        }

        context.HttpContext.Items[HttpContextItemKeys.ClientETag] = expectedVersion;

        await next();
    }
}
