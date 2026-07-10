namespace Utano.API.Filters;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth/setup",
                StringComparison.OrdinalIgnoreCase))
        {
            var expectedKey = configuration["ApiKey"];

            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Setup endpoint is not configured.");
                return;
            }

            if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || providedKey != expectedKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid or missing API key.");
                return;
            }
        }

        await next(context);
    }
}
