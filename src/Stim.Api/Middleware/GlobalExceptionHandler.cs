using System;
using Microsoft.AspNetCore.Diagnostics;

namespace Stim.Api.Middleware;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext()
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails()
            {
                Detail = "An Internal Server Error Has Occured, Please Try Your Request Again Later",
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
            }
        });
    }
}
