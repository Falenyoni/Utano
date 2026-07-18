using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.MarkAsRead;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class MarkNotificationReadEndpoint(INotificationRepository repository) : ControllerBase
{
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(204)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken ct)
    {
        var notification = await repository.GetByIdAsync(id, ct);
        if (notification is null) return NotFound();
        notification.MarkAsRead();
        await repository.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(204)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(ct);
        return NoContent();
    }
}
