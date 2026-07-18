using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.Notifications.Domain.Entities;

public class Notification : AggregateRoot
{
    private Notification() { }

    public Guid RecipientUserId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string SenderName { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public Guid? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }

    public static Notification Create(
        Guid practiceId,
        Guid recipientUserId,
        Guid senderUserId,
        string senderName,
        string title,
        string message,
        string type,
        Guid? referenceId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            RecipientUserId = recipientUserId,
            SenderUserId = senderUserId,
            SenderName = senderName,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
