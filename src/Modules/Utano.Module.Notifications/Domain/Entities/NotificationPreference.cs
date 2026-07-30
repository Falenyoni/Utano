using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.Notifications.Domain.Entities;

public class NotificationPreference : AggregateRoot
{
    private NotificationPreference() { }

    public Guid UserId { get; private set; }
    public bool InAppEnabled { get; private set; } = true;
    public bool EmailEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool WhatsAppEnabled { get; private set; }
    public DateTimeOffset? ConsentRecordedAt { get; private set; }

    public static NotificationPreference CreateDefault(Guid practiceId, Guid userId)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            UserId = userId,
            InAppEnabled = true,
            EmailEnabled = false,
            SmsEnabled = false,
            WhatsAppEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(bool inAppEnabled, bool emailEnabled, bool smsEnabled, bool whatsAppEnabled)
    {
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        WhatsAppEnabled = whatsAppEnabled;

        // Any channel beyond in-app requires an explicit opt-in moment on record - transactional
        // in-app notifications don't need it, external sends to a phone/inbox do.
        if ((emailEnabled || smsEnabled || whatsAppEnabled) && ConsentRecordedAt is null)
            ConsentRecordedAt = DateTimeOffset.UtcNow;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
