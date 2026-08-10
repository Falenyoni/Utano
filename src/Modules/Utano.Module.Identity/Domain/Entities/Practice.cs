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
    public string? LogoBase64 { get; private set; } // deprecated - legacy inline logo storage, superseded by LogoFileId (R2). Kept until existing practices are backfilled; see docs/file-storage.md.
    public Guid? LogoFileId { get; private set; }

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
        SubscriptionTier   = Entities.SubscriptionTier.Professional;
        SubscriptionStatus = Entities.SubscriptionStatus.Trial;
        TrialEndsAt        = DateTimeOffset.UtcNow.AddDays(days);
        UpdatedAt          = DateTimeOffset.UtcNow;
    }

    // Extends from the current TrialEndsAt if it's still in the future (so "give them 2 more
    // weeks" adds cleanly on top of whatever's left), or from now if the trial already lapsed but
    // the status hasn't flipped to Starter yet (LoginUserHandler does that on next login) - either
    // way the new date always lands meaningfully in the future.
    public void ExtendTrial(int additionalDays)
    {
        if (SubscriptionStatus != Entities.SubscriptionStatus.Trial)
            throw new UtanoDomainException("Practice is not currently on a trial.");
        if (additionalDays <= 0)
            throw new UtanoDomainException("Additional days must be positive.");

        var baseline = TrialEndsAt.HasValue && TrialEndsAt.Value > DateTimeOffset.UtcNow
            ? TrialEndsAt.Value
            : DateTimeOffset.UtcNow;

        TrialEndsAt = baseline.AddDays(additionalDays);
        UpdatedAt   = DateTimeOffset.UtcNow;
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

    // logoFileId: null = keep existing logo, clearLogo = true removes it (wins over logoFileId),
    // otherwise a value sets the new logo. Returns the previous LogoFileId so the caller can clean
    // up the now-orphaned R2 object when it changed.
    public Guid? UpdateBranding(string? primaryColor, Guid? logoFileId, bool clearLogo)
    {
        PrimaryColor = primaryColor;

        var previousLogoFileId = LogoFileId;
        if (clearLogo)
            LogoFileId = null;
        else if (logoFileId is not null)
            LogoFileId = logoFileId;

        UpdatedAt = DateTimeOffset.UtcNow;
        return previousLogoFileId != LogoFileId ? previousLogoFileId : null;
    }

    // One-off backfill from the legacy base64 logo into R2 - see the admin migrate-branding-logos
    // endpoint. Not part of the normal branding flow.
    public void SetLogoFileIdFromMigration(Guid logoFileId)
    {
        LogoFileId = logoFileId;
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
