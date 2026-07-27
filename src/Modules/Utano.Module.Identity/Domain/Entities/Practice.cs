using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Identity.Domain.Entities;

public static class SubscriptionTier
{
    public const string Starter      = "Starter";
    public const string Professional = "Professional";
}

public static class SubscriptionStatus
{
    public const string Trial     = "Trial";
    public const string Active    = "Active";
    public const string PastDue   = "PastDue";
    public const string Cancelled = "Cancelled";
}

public class Practice : AggregateRoot
{
    private Practice() { }

    public string Name { get; private set; } = null!;
    public string ContactEmail { get; private set; } = null!;
    public string ContactPhone { get; private set; } = null!;
    public string PhysicalAddress { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool HasDispensary { get; private set; }
    public string? AdhozNumber { get; private set; }
    public string? BpNumber { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? LogoBase64 { get; private set; }

    public string SubscriptionTier { get; private set; } = Entities.SubscriptionTier.Starter;
    public string SubscriptionStatus { get; private set; } = Entities.SubscriptionStatus.Trial;
    public DateTimeOffset? TrialEndsAt { get; private set; }
    public DateTimeOffset? SubscriptionExpiresAt { get; private set; }

    public bool IsSubscriptionActive(DateTimeOffset now) => SubscriptionStatus switch
    {
        Entities.SubscriptionStatus.Active  => SubscriptionExpiresAt is null || SubscriptionExpiresAt > now,
        Entities.SubscriptionStatus.Trial   => TrialEndsAt.HasValue && TrialEndsAt > now,
        _                                   => false
    };

    public void StartTrial(int days = 30)
    {
        SubscriptionTier   = Entities.SubscriptionTier.Starter;
        SubscriptionStatus = Entities.SubscriptionStatus.Trial;
        TrialEndsAt        = DateTimeOffset.UtcNow.AddDays(days);
        UpdatedAt          = DateTimeOffset.UtcNow;
    }

    public void SetSubscription(string tier, string status, DateTimeOffset? expiresAt)
    {
        SubscriptionTier      = tier;
        SubscriptionStatus    = status;
        SubscriptionExpiresAt = expiresAt;
        UpdatedAt             = DateTimeOffset.UtcNow;
    }

    public void MarkPastDue()
    {
        SubscriptionStatus = Entities.SubscriptionStatus.PastDue;
        UpdatedAt          = DateTimeOffset.UtcNow;
    }

    public void CancelSubscription()
    {
        SubscriptionStatus = Entities.SubscriptionStatus.Cancelled;
        UpdatedAt          = DateTimeOffset.UtcNow;
    }

    // logoBase64: null = keep existing, "" = clear logo, any other value = update logo
    public void UpdateBranding(string? primaryColor, string? logoBase64)
    {
        PrimaryColor = primaryColor;
        if (logoBase64 is not null)
            LogoBase64 = logoBase64 == string.Empty ? null : logoBase64;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetHasDispensary(bool value)
    {
        HasDispensary = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string contactEmail, string contactPhone, string physicalAddress, string? adhozNumber, string? bpNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Practice name is required.");
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new UtanoDomainException("Contact email is required.");
        Name = name.Trim();
        ContactEmail = contactEmail.Trim().ToLower();
        ContactPhone = contactPhone.Trim();
        PhysicalAddress = physicalAddress.Trim();
        AdhozNumber = string.IsNullOrWhiteSpace(adhozNumber) ? null : adhozNumber.Trim();
        BpNumber = string.IsNullOrWhiteSpace(bpNumber) ? null : bpNumber.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Practice Create(string name, string contactEmail,
        string contactPhone, string physicalAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Practice name is required.");

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new UtanoDomainException("Contact email is required.");

        var id = Guid.NewGuid();

        return new Practice
        {
            Id = id,
            PracticeId = id,
            Name = name.Trim(),
            ContactEmail = contactEmail.Trim().ToLower(),
            ContactPhone = contactPhone.Trim(),
            PhysicalAddress = physicalAddress.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
