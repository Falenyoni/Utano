using Microsoft.EntityFrameworkCore;
using Utano.Module.Identity.DatabaseMappings;
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

    public async Task InvokeAsync(HttpContext context, IdentityDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (SkippedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || !context.User.Identity?.IsAuthenticated == true)
        {
            await next(context);
            return;
        }

        var practiceIdClaim = context.User.FindFirst("PracticeId")?.Value;
        if (!Guid.TryParse(practiceIdClaim, out var practiceId))
        {
            await next(context);
            return;
        }

        var practice = await db.Practices
            .AsNoTracking()
            .Where(p => p.Id == practiceId)
            .Select(p => new { p.SubscriptionStatus, p.TrialEndsAt, p.SubscriptionExpiresAt })
            .FirstOrDefaultAsync(context.RequestAborted);

        if (practice is null)
        {
            await next(context);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var isBlocked = practice.SubscriptionStatus switch
        {
            SubscriptionStatus.Cancelled => true,
            SubscriptionStatus.PastDue   => true,
            SubscriptionStatus.Trial     => !practice.TrialEndsAt.HasValue || practice.TrialEndsAt <= now,
            SubscriptionStatus.Active    => practice.SubscriptionExpiresAt.HasValue && practice.SubscriptionExpiresAt <= now,
            _                            => false
        };

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
