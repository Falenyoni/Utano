using Utano.Module.Identity.Domain.Entities;

namespace Utano.API.Filters;

public class SubscriptionMiddleware(RequestDelegate next)
{
    private static readonly string[] SkippedPrefixes =
    [
        "/api/auth",
        "/api/admin",
        "/health",
        "/swagger",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (SkippedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var status = context.User.FindFirst("SubscriptionStatus")?.Value;

        // No claim means old token — let it through, will be refreshed on next login
        if (string.IsNullOrEmpty(status))
        {
            await next(context);
            return;
        }

        var isBlocked = status is SubscriptionStatus.Cancelled or SubscriptionStatus.PastDue;

        if (isBlocked)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"subscription_required","message":"Your subscription has expired or been cancelled. Please contact support."}""");
            return;
        }

        await next(context);
    }
}
