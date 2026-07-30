using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Core.Exceptions;

namespace Utano.API.Filters;

// Without this, every UtanoDomainException (validation failures, business-rule violations)
// falls through to Kestrel's developer exception page - a raw 500 with a full stack trace
// in the response body, instead of the clean 400 + message every handler's throw already
// intends. The frontend already expects a ProblemDetails-shaped { detail } response for
// this exact case; this is what actually produces it.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is UtanoDomainException domainException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = domainException.Message,
            }, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "Please try again or contact support if the problem persists.",
        }, cancellationToken);
        return true;
    }
}
