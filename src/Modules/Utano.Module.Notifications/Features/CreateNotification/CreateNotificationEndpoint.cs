using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Interfaces;

namespace Utano.Module.Notifications.Features.CreateNotification;

public record CreateNotificationRequest(
    Guid RecipientUserId,
    string Title,
    string Message,
    string Type,
    Guid? ReferenceId);

public record CreateNotificationResponse(Guid Id);

[ApiController]
[Route("api/notifications")]
[Authorize]
public class CreateNotificationEndpoint(
    INotificationRepository repository,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateNotificationResponse), 201)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken ct)
    {
        var notification = Notification.Create(
            currentUserService.PracticeId,
            request.RecipientUserId,
            currentUserService.UserId,
            currentUserService.FullName,
            request.Title,
            request.Message,
            request.Type,
            request.ReferenceId);

        await repository.AddAsync(notification, ct);
        await repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Create), new CreateNotificationResponse(notification.Id));
    }
}
