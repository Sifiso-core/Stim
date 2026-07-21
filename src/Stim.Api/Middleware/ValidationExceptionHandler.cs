using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Stim.Api.Middleware;

public class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not FluentValidation.ValidationException validationException)
        {
            return false;
        }

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            }
        };

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key.ToLowerInvariant(),
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
        context.ProblemDetails.Extensions.Add("errors", errors);

        return await problemDetailsService.TryWriteAsync(context);
    }
}
