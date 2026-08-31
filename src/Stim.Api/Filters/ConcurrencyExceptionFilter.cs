using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
namespace Stim.Api.Filters;

public sealed class ConcurrencyExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException)
        {
            return;
        }

        context.Result = new ObjectResult(new
        {
            error = "The resource has been modified by another user. Fetch the latest version and retry."
        })

        {
            StatusCode = StatusCodes.Status412PreconditionFailed
        };

        context.ExceptionHandled = true;
    }
}

