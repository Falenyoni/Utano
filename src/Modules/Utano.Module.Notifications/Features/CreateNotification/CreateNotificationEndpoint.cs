using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;
using Utano.Module.Notifications.Domain.Enums;
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
    IUserPracticeValidator userPracticeValidator,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateNotificationResponse), 201)]
    [ProducesResponseType(400)]
    [Tags("Notifications Module")]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, ignoreCase: true, out var type))
            return BadRequest($"Unknown notification type '{request.Type}'.");

        var recipientInPractice = await userPracticeValidator.IsUserInPracticeAsync(
            request.RecipientUserId, currentUserService.PracticeId, ct);
        if (!recipientInPractice)
            return BadRequest("Recipient must belong to your practice.");

        var notification = Notification.Create(
            currentUserService.PracticeId,
            request.RecipientUserId,
            currentUserService.UserId,
            currentUserService.FullName,
            request.Title,
            request.Message,
            type,
            request.ReferenceId);

        await repository.AddAsync(notification, ct);
        await repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Create), new CreateNotificationResponse(notification.Id));
    }
}
