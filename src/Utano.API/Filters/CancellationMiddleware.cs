namespace Utano.API.Filters;

public class CancellationMiddleware(RequestDelegate next, ILogger<CancellationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
    }
}
