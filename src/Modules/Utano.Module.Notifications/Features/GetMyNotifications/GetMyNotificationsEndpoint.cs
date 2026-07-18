using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.GetMyNotifications;

public record NotificationResponse(
    Guid Id,
    string SenderName,
    string Title,
    string Message,
    string Type,
    Guid? ReferenceId,
    bool IsRead,
    DateTimeOffset CreatedAt);

[ApiController]
[Route("api/notifications")]
[Authorize]
public class GetMyNotificationsEndpoint(INotificationRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), 200)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var notifications = await repository.GetMyNotificationsAsync(30, ct);
        var result = notifications.Select(n => new NotificationResponse(
            n.Id, n.SenderName, n.Title, n.Message, n.Type, n.ReferenceId, n.IsRead, n.CreatedAt));
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), 200)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await repository.GetUnreadCountAsync(ct);
        return Ok(count);
    }
}
