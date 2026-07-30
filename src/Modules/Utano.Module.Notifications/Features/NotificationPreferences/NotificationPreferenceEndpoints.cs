using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.DatabaseMappings;
using Utano.Module.Notifications.Domain.Entities;

namespace Utano.Module.Notifications.Features.NotificationPreferences;

public record NotificationPreferenceResponse(
    bool InAppEnabled, bool EmailEnabled, bool SmsEnabled, bool WhatsAppEnabled,
    DateTimeOffset? ConsentRecordedAt);

public record UpdateNotificationPreferenceRequest(
    bool InAppEnabled, bool EmailEnabled, bool SmsEnabled, bool WhatsAppEnabled);

[ApiController]
[Route("api/notifications/preferences")]
[Authorize]
public class NotificationPreferenceEndpoints(
    NotificationsDbContext db,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationPreferenceResponse), 200)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var preference = await db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == currentUserService.UserId, ct);

        // No row yet just means the defaults apply - don't persist one until the user
        // actually changes something.
        if (preference is null)
            return Ok(new NotificationPreferenceResponse(true, false, false, false, null));

        return Ok(new NotificationPreferenceResponse(
            preference.InAppEnabled, preference.EmailEnabled,
            preference.SmsEnabled, preference.WhatsAppEnabled,
            preference.ConsentRecordedAt));
    }

    [HttpPut]
    [ProducesResponseType(204)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateNotificationPreferenceRequest request, CancellationToken ct)
    {
        var preference = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == currentUserService.UserId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(
                currentUserService.PracticeId, currentUserService.UserId);
            db.NotificationPreferences.Add(preference);
        }

        preference.Update(
            request.InAppEnabled, request.EmailEnabled,
            request.SmsEnabled, request.WhatsAppEnabled);

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
